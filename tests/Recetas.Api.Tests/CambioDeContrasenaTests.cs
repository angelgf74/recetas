using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Cambiar la contraseña con la sesión iniciada, sabiendo la actual.
/// </summary>
[Trait("Categoria", "Integracion")]
public class CambioDeContrasenaTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string ContrasenaActual = "una-contrasena-larga";
    private const string ContrasenaNueva = "otra-contrasena-larga-y-distinta";

    [Fact]
    public async Task SinSesion_DevuelveNoAutorizado()
    {
        var anonimo = api.CreateClient();

        var respuesta = await anonimo.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = ContrasenaActual,
            ContrasenaNueva = ContrasenaNueva
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConLaActualCorrecta_CambiaYPermiteEntrarConLaNueva()
    {
        var (cliente, correo) = await CuentaAsync();

        var respuesta = await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = ContrasenaActual,
            ContrasenaNueva = ContrasenaNueva
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var conLaNueva = await api.CreateClient().PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaNueva });
        Assert.Equal(HttpStatusCode.OK, conLaNueva.StatusCode);

        var conLaAntigua = await api.CreateClient().PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaActual });
        Assert.Equal(HttpStatusCode.Unauthorized, conLaAntigua.StatusCode);
    }

    [Fact]
    public async Task ConLaActualIncorrecta_DevuelveNoAutorizadoYNoCambiaNada()
    {
        var (cliente, correo) = await CuentaAsync();

        var respuesta = await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = "esta no es la contraseña",
            ContrasenaNueva = ContrasenaNueva
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var conLaOriginal = await api.CreateClient().PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaActual });
        Assert.Equal(HttpStatusCode.OK, conLaOriginal.StatusCode);
    }

    [Fact]
    public async Task ContrasenaNuevaDemasiadoCorta_DevuelveDatosNoValidos()
    {
        var (cliente, _) = await CuentaAsync();

        var respuesta = await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = ContrasenaActual,
            ContrasenaNueva = "corta"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task AlCambiar_LlegaElCorreoDeAviso()
    {
        var (cliente, correo) = await CuentaAsync();

        await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = ContrasenaActual,
            ContrasenaNueva = ContrasenaNueva
        });

        Assert.Contains(correo, api.Correo.ConfirmacionesDeCambioDeContrasena);
    }

    // ------------------------------------------------------------- Auxiliares

    private async Task<(HttpClient Cliente, string Correo)> CuentaAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"cambio-{Guid.NewGuid():N}@ejemplo.com";

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = ContrasenaActual
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = ContrasenaActual });

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        Assert.NotNull(acceso);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);

        return (cliente, correo);
    }
}
