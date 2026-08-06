using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recetas.Dominio.Puertos;
using Recetas.Infraestructura.Correo;
using Recetas.Infraestructura.Fotos;
using Recetas.Infraestructura.Importacion;
using Recetas.Infraestructura.Persistencia;
using Recetas.Infraestructura.Seguridad;
using Recetas.Infraestructura.Tiempo;

namespace Recetas.Infraestructura;

/// <summary>
/// Registro de los adaptadores concretos. Es el único punto donde la API
/// conoce tipos de infraestructura: los endpoints solo ven puertos del dominio.
/// </summary>
public static class InyeccionDeDependencias
{
    public static IServiceCollection AnadirInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var cadenaDeConexion = configuracion.GetConnectionString("Recetas")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'Recetas'. Defínela en appsettings.Development.json " +
                "o en la variable de entorno ConnectionStrings__Recetas (ver appsettings.Example.json).");

        servicios.AddDbContext<RecetasDbContext>(opciones => opciones.UseNpgsql(cadenaDeConexion));

        servicios.AddScoped<IComprobadorDeAlmacen, ComprobadorDeAlmacenEf>();
        servicios.AddScoped<IRepositorioDeUsuarios, RepositorioDeUsuariosEf>();
        servicios.AddScoped<IRepositorioDeSolicitudesDeRegistro, RepositorioDeSolicitudesDeRegistroEf>();
        servicios.AddScoped<IRepositorioDeSolicitudesDeContrasena, RepositorioDeSolicitudesDeContrasenaEf>();
        servicios.AddScoped<IRepositorioDeRecetas, RepositorioDeRecetasEf>();
        servicios.AddScoped<IRepositorioDeIngredientes, RepositorioDeIngredientesEf>();
        servicios.AddScoped<IRepositorioDeDenuncias, RepositorioDeDenunciasEf>();

        servicios.AddSingleton<IReloj, RelojDelSistema>();
        servicios.AddSingleton<IHasheadorDeContrasenas, HasheadorPbkdf2>();
        servicios.AddSingleton<IGeneradorDeTokens, GeneradorDeTokensAleatorios>();
        servicios.AddScoped<IEmisorDeAcceso, EmisorDeAccesoJwt>();

        servicios.Configure<OpcionesDeFotos>(configuracion.GetSection(OpcionesDeFotos.Seccion));
        servicios.AddSingleton<IAlmacenDeFotos, AlmacenDeFotosEnDisco>();
        servicios.AddSingleton<ILimpiadorDeImagenes, LimpiadorDeImagenesConImageSharp>();
        servicios.AddSingleton<IEscaladorDeImagenes, EscaladorDeImagenesConImageSharp>();

        servicios.Configure<OpcionesDeImportacion>(configuracion.GetSection(OpcionesDeImportacion.Seccion));
        servicios.AddSingleton<IDescargadorDePaginas, DescargadorDePaginasSeguro>();

        AnadirOpcionesDeJwt(servicios, configuracion);
        AnadirCorreo(servicios, configuracion);

        return servicios;
    }

    private static void AnadirOpcionesDeJwt(IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = new OpcionesDeJwt();
        configuracion.GetSection(OpcionesDeJwt.Seccion).Bind(opciones);

        // Se valida al arrancar, no en la primera petición: un despliegue con la
        // clave de firma ausente o demasiado corta no debe llegar a atender tráfico.
        opciones.Validar();

        servicios.Configure<OpcionesDeJwt>(configuracion.GetSection(OpcionesDeJwt.Seccion));
    }

    private static void AnadirCorreo(IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = new OpcionesDeCorreo();
        configuracion.GetSection(OpcionesDeCorreo.Seccion).Bind(opciones);
        opciones.Validar();

        servicios.Configure<OpcionesDeCorreo>(configuracion.GetSection(OpcionesDeCorreo.Seccion));

        if (!opciones.UsarBrevo)
        {
            // Por defecto no se envía correo real. Brevo exige activación explícita.
            servicios.AddScoped<IEnviadorDeCorreo, EnviadorDeCorreoDeConsola>();
            return;
        }

        servicios.AddHttpClient<IEnviadorDeCorreo, EnviadorDeCorreoBrevo>(cliente =>
        {
            cliente.BaseAddress = new Uri("https://api.brevo.com/");
            cliente.DefaultRequestHeaders.Add("api-key", opciones.ClaveDeApi);
            cliente.DefaultRequestHeaders.Add("accept", "application/json");
            cliente.Timeout = TimeSpan.FromSeconds(15);
        });
    }
}
