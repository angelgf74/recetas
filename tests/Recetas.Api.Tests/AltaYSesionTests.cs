using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Recorre el alta completa contra PostgreSQL real: solicitud, correo, elección
/// de contraseña, inicio de sesión y uso del token en un endpoint protegido.
/// </summary>
[Trait("Categoria", "Integracion")]
public class AltaYSesionTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string ContrasenaValida = "una-contrasena-larga";

    [Fact]
    public async Task AltaCompleta_PermiteIniciarSesionYUsarElToken()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();

        var solicitud = await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });
        Assert.Equal(HttpStatusCode.OK, solicitud.StatusCode);

        var token = api.Correo.TokenEnviadoA(correo);

        var completar = await cliente.PostAsJsonAsync("/registro/completar",
            new PeticionDeCompletarRegistro { Token = token, Contrasena = ContrasenaValida });
        Assert.Equal(HttpStatusCode.OK, completar.StatusCode);

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaValida });
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        Assert.NotNull(acceso);
        Assert.False(string.IsNullOrWhiteSpace(acceso.Token));

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);
        var yo = await cliente.GetAsync("/yo");

        Assert.Equal(HttpStatusCode.OK, yo.StatusCode);
        var identidad = await yo.Content.ReadFromJsonAsync<RespuestaDeIdentidad>();
        Assert.NotNull(identidad);
        Assert.Equal(correo, identidad.Correo);
    }

    [Fact]
    public async Task Yo_Responde401SinToken()
    {
        var respuesta = await api.CreateClient().GetAsync("/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Yo_Responde401ConUnTokenInventado()
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "esto.no.es-un-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/yo")).StatusCode);
    }

    /// <summary>El criterio central del paso 1: la respuesta no puede delatar qué correos existen.</summary>
    [Fact]
    public async Task Solicitud_RespondeIgual_ExistaONoLaCuenta()
    {
        var cliente = api.CreateClient();
        var registrado = CorreoUnico();

        await CrearCuentaAsync(cliente, registrado);

        var conCuenta = await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = registrado });
        var sinCuenta = await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = CorreoUnico() });

        Assert.Equal(conCuenta.StatusCode, sinCuenta.StatusCode);
        Assert.Equal(
            await conCuenta.Content.ReadAsStringAsync(),
            await sinCuenta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ElToken_NoSirveDosVeces()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });
        var token = api.Correo.TokenEnviadoA(correo);

        var peticion = new PeticionDeCompletarRegistro { Token = token, Contrasena = ContrasenaValida };

        Assert.Equal(HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync("/registro/completar", peticion)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/registro/completar", peticion)).StatusCode);
    }

    [Fact]
    public async Task ContrasenaCorta_NoCreaLaCuenta()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });
        var token = api.Correo.TokenEnviadoA(correo);

        var completar = await cliente.PostAsJsonAsync("/registro/completar",
            new PeticionDeCompletarRegistro { Token = token, Contrasena = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, completar.StatusCode);

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = "corta" });
        Assert.Equal(HttpStatusCode.Unauthorized, sesion.StatusCode);
    }

    [Fact]
    public async Task SolicitudPendiente_NoPermiteIniciarSesion()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();

        // Se pide el alta pero no se completa: esa dirección todavía no es una cuenta.
        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaValida });

        Assert.Equal(HttpStatusCode.Unauthorized, sesion.StatusCode);
    }

    [Fact]
    public async Task CredencialesIncorrectas_RespondenIgual_ExistaONoElCorreo()
    {
        var cliente = api.CreateClient();
        var registrado = CorreoUnico();
        await CrearCuentaAsync(cliente, registrado);

        var contrasenaMala = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = registrado, Contrasena = "otra-contrasena-larga" });
        var correoInexistente = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = CorreoUnico(), Contrasena = ContrasenaValida });

        Assert.Equal(contrasenaMala.StatusCode, correoInexistente.StatusCode);
        Assert.Equal(
            await contrasenaMala.Content.ReadAsStringAsync(),
            await correoInexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CorreoInvalido_Responde400()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = "esto-no-es-un-correo" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElErrorDeEnlaceInvalido_NoDevuelveElToken()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/registro/completar",
            new PeticionDeCompletarRegistro { Token = "token-inventado", Contrasena = ContrasenaValida });

        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        // Devolver el token en el error lo dejaría escrito en cualquier log de cliente.
        Assert.DoesNotContain("token-inventado", cuerpo);
    }

    private static string CorreoUnico() => $"usuario-{Guid.NewGuid():N}@ejemplo.com";

    private async Task CrearCuentaAsync(HttpClient cliente, string correo)
    {
        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar",
            new PeticionDeCompletarRegistro
            {
                Token = api.Correo.TokenEnviadoA(correo),
                Contrasena = ContrasenaValida
            });
    }
}
