using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Recorre el restablecimiento completo contra PostgreSQL real: alta, solicitud,
/// correo, elección de contraseña nueva e inicio de sesión con ella.
/// </summary>
[Trait("Categoria", "Integracion")]
public class RecuperarContrasenaTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string ContrasenaOriginal = "una-contrasena-larga";
    private const string ContrasenaNueva = "otra-contrasena-mas-larga";

    [Fact]
    public async Task RestablecimientoCompleto_PermiteEntrarConLaContrasenaNueva()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        var solicitud = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });
        Assert.Equal(HttpStatusCode.OK, solicitud.StatusCode);

        var token = api.Correo.TokenDeContrasenaEnviadoA(correo);

        var restablecer = await cliente.PostAsJsonAsync("/contrasena/restablecer",
            new PeticionDeRestablecerContrasena { Token = token, Contrasena = ContrasenaNueva });
        Assert.Equal(HttpStatusCode.OK, restablecer.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await IniciarSesionAsync(cliente, correo, ContrasenaNueva)).StatusCode);
    }

    /// <summary>Criterio de aceptación explícito: la contraseña anterior deja de servir.</summary>
    [Fact]
    public async Task DespuesDeRestablecer_LaContrasenaAnteriorYaNoVale()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        // Antes sí valía: sin esta comprobación el test podría pasar por un
        // montaje mal hecho en lugar de por el cambio.
        Assert.Equal(HttpStatusCode.OK,
            (await IniciarSesionAsync(cliente, correo, ContrasenaOriginal)).StatusCode);

        await RestablecerAsync(cliente, correo, ContrasenaNueva);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await IniciarSesionAsync(cliente, correo, ContrasenaOriginal)).StatusCode);
    }

    /// <summary>El criterio central del paso 1: la respuesta no puede delatar qué correos existen.</summary>
    [Fact]
    public async Task Solicitud_RespondeIgual_ExistaONoLaCuenta()
    {
        var cliente = api.CreateClient();
        var registrado = CorreoUnico();
        await CrearCuentaAsync(cliente, registrado);

        var conCuenta = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = registrado });
        var sinCuenta = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = CorreoUnico() });

        Assert.Equal(conCuenta.StatusCode, sinCuenta.StatusCode);
        Assert.Equal(
            await conCuenta.Content.ReadAsStringAsync(),
            await sinCuenta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ACorreoSinCuenta_NoSeLeEnviaNada()
    {
        var cliente = api.CreateClient();
        var desconocido = CorreoUnico();

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = desconocido });

        Assert.DoesNotContain(api.Correo.EnlacesDeContrasena, envio => envio.Destinatario == desconocido);
    }

    /// <summary>
    /// Un alta a medias no es una cuenta: hasta completarla no hay contraseña
    /// que restablecer, así que tampoco enlace.
    /// </summary>
    [Fact]
    public async Task ConAltaPendienteSinCompletar_NoSeEnviaEnlace()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        var solicitud = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

        Assert.Equal(HttpStatusCode.OK, solicitud.StatusCode);
        Assert.DoesNotContain(api.Correo.EnlacesDeContrasena, envio => envio.Destinatario == correo);
    }

    [Fact]
    public async Task ElToken_NoSirveDosVeces()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

        var peticion = new PeticionDeRestablecerContrasena
        {
            Token = api.Correo.TokenDeContrasenaEnviadoA(correo),
            Contrasena = ContrasenaNueva
        };

        Assert.Equal(HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync("/contrasena/restablecer", peticion)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/contrasena/restablecer", peticion)).StatusCode);
    }

    [Fact]
    public async Task PedirloDeNuevo_InvalidaElEnlaceAnterior()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });
        var primerToken = api.Correo.TokenDeContrasenaEnviadoA(correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });
        var segundoToken = api.Correo.TokenDeContrasenaEnviadoA(correo);

        Assert.NotEqual(primerToken, segundoToken);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/contrasena/restablecer",
                new PeticionDeRestablecerContrasena
                {
                    Token = primerToken,
                    Contrasena = ContrasenaNueva
                })).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync("/contrasena/restablecer",
                new PeticionDeRestablecerContrasena
                {
                    Token = segundoToken,
                    Contrasena = ContrasenaNueva
                })).StatusCode);
    }

    [Fact]
    public async Task ContrasenaCorta_NoCambiaNadaNiQuemaElEnlace()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });
        var token = api.Correo.TokenDeContrasenaEnviadoA(correo);

        var corta = await cliente.PostAsJsonAsync("/contrasena/restablecer",
            new PeticionDeRestablecerContrasena { Token = token, Contrasena = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);

        // La contraseña original sigue funcionando…
        Assert.Equal(HttpStatusCode.OK,
            (await IniciarSesionAsync(cliente, correo, ContrasenaOriginal)).StatusCode);

        // …y el enlace no se ha gastado por un error de tecleo.
        Assert.Equal(HttpStatusCode.OK,
            (await cliente.PostAsJsonAsync("/contrasena/restablecer",
                new PeticionDeRestablecerContrasena
                {
                    Token = token,
                    Contrasena = ContrasenaNueva
                })).StatusCode);
    }

    [Fact]
    public async Task TokenInventado_RespondeIgualQueUnoYaUsado()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

        var usado = new PeticionDeRestablecerContrasena
        {
            Token = api.Correo.TokenDeContrasenaEnviadoA(correo),
            Contrasena = ContrasenaNueva
        };
        await cliente.PostAsJsonAsync("/contrasena/restablecer", usado);

        var reintento = await cliente.PostAsJsonAsync("/contrasena/restablecer", usado);
        var inventado = await cliente.PostAsJsonAsync("/contrasena/restablecer",
            new PeticionDeRestablecerContrasena { Token = "token-inventado", Contrasena = ContrasenaNueva });

        Assert.Equal(reintento.StatusCode, inventado.StatusCode);
        Assert.Equal(
            await reintento.Content.ReadAsStringAsync(),
            await inventado.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CorreoInvalido_Responde400()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = "esto-no-es-un-correo" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElErrorDeEnlaceInvalido_NoDevuelveElToken()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/contrasena/restablecer",
            new PeticionDeRestablecerContrasena { Token = "token-inventado", Contrasena = ContrasenaNueva });

        Assert.DoesNotContain("token-inventado", await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// El enlace apunta a la pantalla de elegir contraseña, no a la de pedirla:
    /// si aterrizara en otro sitio, el usuario tendría que volver a empezar.
    /// </summary>
    [Fact]
    public async Task ElEnlaceApuntaALaPantallaDeElegirContrasena()
    {
        var cliente = api.CreateClient();
        var correo = CorreoUnico();
        await CrearCuentaAsync(cliente, correo);

        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

        var enlace = api.Correo.EnlacesDeContrasena.Last(envio => envio.Destinatario == correo).Enlace;

        Assert.StartsWith("https://recetas.test/contrasena/nueva?token=", enlace);
    }

    private static string CorreoUnico() => $"usuario-{Guid.NewGuid():N}@ejemplo.com";

    private static Task<HttpResponseMessage> IniciarSesionAsync(
        HttpClient cliente,
        string correo,
        string contrasena) =>
        cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = contrasena });

    private async Task CrearCuentaAsync(HttpClient cliente, string correo)
    {
        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        var completar = await cliente.PostAsJsonAsync("/registro/completar",
            new PeticionDeCompletarRegistro
            {
                Token = api.Correo.TokenEnviadoA(correo),
                Contrasena = ContrasenaOriginal
            });

        // Si el alta falla, los fallos posteriores serían desconcertantes.
        Assert.Equal(HttpStatusCode.OK, completar.StatusCode);
    }

    private async Task RestablecerAsync(HttpClient cliente, string correo, string contrasenaNueva)
    {
        await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

        var respuesta = await cliente.PostAsJsonAsync("/contrasena/restablecer",
            new PeticionDeRestablecerContrasena
            {
                Token = api.Correo.TokenDeContrasenaEnviadoA(correo),
                Contrasena = contrasenaNueva
            });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
