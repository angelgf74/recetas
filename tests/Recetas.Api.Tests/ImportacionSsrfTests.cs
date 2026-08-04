using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Ejercita el descargador <b>de verdad</b>, sin sustituirlo por un doble: es la
/// única forma de comprobar que el filtro de direcciones está montado en la
/// aplicación real y no solo probado por su cuenta.
/// </summary>
/// <remarks>
/// Lo que se verifica es que la API no sirve para alcanzar la red interna del
/// servidor, y que además <b>no lo cuenta</b>: el error de una dirección interna es
/// idéntico al de un dominio que no existe. Si difirieran, este endpoint sería un
/// escáner de red manejado por cualquiera con una cuenta.
/// </remarks>
[Trait("Categoria", "Integracion")]
public class ImportacionSsrfTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Theory]
    // Bucle local: aquí escuchan Kestrel y, en el servidor, PostgreSQL.
    [InlineData("http://127.0.0.1/receta")]
    [InlineData("http://127.0.0.1:5432/receta")]
    [InlineData("http://localhost:54009/receta")]
    [InlineData("http://[::1]/receta")]

    // Red privada.
    [InlineData("http://10.0.0.1/receta")]
    [InlineData("http://192.168.1.1/receta")]
    [InlineData("http://172.16.0.1/receta")]

    // Metadatos de nube: credenciales de la máquina.
    [InlineData("http://169.254.169.254/latest/meta-data/")]

    // La IPv4 disfrazada de IPv6, por si el filtro solo mirara una familia.
    [InlineData("http://[::ffff:127.0.0.1]/receta")]
    public async Task UnaDireccionInterna_NoSeAlcanza(string direccion)
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await ImportarAsync(cliente, direccion);

        Assert.Equal(HttpStatusCode.BadGateway, respuesta.StatusCode);
    }

    /// <summary>
    /// El mensaje no puede delatar la diferencia entre "hay algo escuchando ahí
    /// dentro" y "ese dominio no existe".
    /// </summary>
    [Fact]
    public async Task ElErrorDeUnaDireccionInterna_EsIgualQueElDeUnDominioQueNoExiste()
    {
        var cliente = await ClienteAutenticadoAsync();

        var interna = await ImportarAsync(cliente, "http://127.0.0.1:5432/receta");
        var inexistente = await ImportarAsync(cliente, "http://no-existe-este-dominio.invalid/receta");

        Assert.Equal(interna.StatusCode, inexistente.StatusCode);
        Assert.Equal(
            await interna.Content.ReadAsStringAsync(),
            await inexistente.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// El cuerpo del error no debe llevar la dirección ni el motivo técnico: sería
    /// la misma fuga por otra vía.
    /// </summary>
    [Fact]
    public async Task ElErrorNoRevelaDetallesDeLaRed()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await ImportarAsync(cliente, "http://169.254.169.254/latest/meta-data/");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.DoesNotContain("169.254", cuerpo);
        Assert.DoesNotContain("público", cuerpo);
    }

    // ------------------------------------------------------------ Utilidades

    private static Task<HttpResponseMessage> ImportarAsync(HttpClient cliente, string direccion) =>
        cliente.PostAsJsonAsync("/recetas/importaciones", new PeticionDeImportacion { Direccion = direccion });

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"ssrf-{Guid.NewGuid():N}@ejemplo.com";

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = Contrasena
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = Contrasena });

        Assert.True(
            sesion.StatusCode == HttpStatusCode.OK,
            $"No se pudo preparar el usuario de prueba: /sesiones respondió {(int)sesion.StatusCode}.");

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", acceso!.Token);

        return cliente;
    }
}
