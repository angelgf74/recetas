using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Recetas.Api.Endpoints;
using Recetas.Aplicacion;
using Recetas.Aplicacion.Salud;
using Recetas.Contratos.Salud;
using Recetas.Dominio.Salud;
using Recetas.Infraestructura;
using Recetas.Infraestructura.Seguridad;

var constructor = WebApplication.CreateBuilder(args);

// Integración con systemd: la unidad usa Type=notify, así que systemd espera a
// que la aplicación avise de que ya está lista antes de darla por arrancada.
// Sin esto el servicio se quedaría en "activating" hasta agotar el tiempo de
// espera. Fuera de systemd no hace nada, así que no molesta en desarrollo.
constructor.Host.UseSystemd();

constructor.Services.AnadirAplicacion();
constructor.Services.AnadirInfraestructura(constructor.Configuration);
constructor.Services.AnadirLimitesDePeticiones(constructor.Configuration);

var opcionesDeJwt = new OpcionesDeJwt();
constructor.Configuration.GetSection(OpcionesDeJwt.Seccion).Bind(opcionesDeJwt);
opcionesDeJwt.Validar();

constructor.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opcionesDeJwt.Emisor,
            ValidateAudience = true,
            ValidAudience = opcionesDeJwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesDeJwt.ClaveDeFirma)),
            ValidateLifetime = true,
            // Sin margen de cortesía: la caducidad significa lo que dice.
            ClockSkew = TimeSpan.Zero
        };
    });

constructor.Services.AddAuthorization();

// La web WebAssembly se sirve desde otro origen, así que necesita CORS explícito.
const string PoliticaDeLaWeb = "web";
var origenesPermitidos = constructor.Configuration
    .GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:5200"];

constructor.Services.AddCors(opciones =>
    opciones.AddPolicy(PoliticaDeLaWeb, politica => politica
        .WithOrigins(origenesPermitidos)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Detrás de nginx y del túnel de Cloudflare, la IP que ve Kestrel es la del
// proxy local. Sin esto, el limitador de peticiones metería a todos los usuarios
// en el mismo cubo y bastaría un visitante activo para bloquear el alta a los
// demás.
constructor.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Un único salto: nginx. Es él quien SOBRESCRIBE X-Forwarded-For con la
    // cabecera de Cloudflare, así que la lista nunca debe tener más de un valor.
    // Aceptar más permitiría a un cliente inyectar entradas falsas por la
    // izquierda y falsear su IP para saltarse el límite de peticiones.
    opciones.ForwardLimit = 1;

    // Solo se confía en el nginx local. Por defecto ASP.NET Core ya restringe a
    // loopback, pero se deja explícito para que quede constancia de la decisión.
    opciones.KnownProxies.Clear();
    opciones.KnownProxies.Add(IPAddress.Loopback);
    opciones.KnownProxies.Add(IPAddress.IPv6Loopback);
});

var aplicacion = constructor.Build();

// Lo primero de la cadena: todo lo que venga después (CORS, limitador,
// autenticación, logs) debe ver ya la IP y el esquema reales.
aplicacion.UseForwardedHeaders();

aplicacion.UseCors(PoliticaDeLaWeb);
aplicacion.UseRateLimiter();
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

aplicacion.MapGet("/salud", async (ConsultarSalud consultarSalud, CancellationToken cancelacion) =>
{
    var estado = await consultarSalud.EjecutarAsync(cancelacion);
    var correcto = estado == EstadoDeSalud.Correcto;

    var respuesta = new RespuestaDeSalud(
        Estado: correcto ? "correcto" : "degradado",
        BaseDeDatos: correcto);

    // 503 en lugar de 200 cuando la base de datos no responde: así el túnel,
    // una sonda externa o un despliegue pueden distinguir "vivo" de "operativo".
    return correcto
        ? Results.Ok(respuesta)
        : Results.Json(respuesta, statusCode: StatusCodes.Status503ServiceUnavailable);
});

aplicacion.MapearEndpointsDeRegistro();
aplicacion.MapearEndpointsDeContrasenas();
aplicacion.MapearEndpointsDeSesiones();
aplicacion.MapearEndpointsDeRecetas();
aplicacion.MapearEndpointsDeFotos();

await aplicacion.RunAsync();

/// <summary>
/// Punto de entrada expuesto para que los tests de integración puedan
/// arrancar la aplicación con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
