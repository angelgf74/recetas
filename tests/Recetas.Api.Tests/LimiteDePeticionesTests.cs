using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Registro;

namespace Recetas.Api.Tests;

/// <summary>
/// Fixture con el límite de registros bajado a dos, para comprobar el rechazo
/// sin tener que lanzar cientos de peticiones.
/// </summary>
public sealed class ApiConLimiteEstrechoFixture : ApiConPostgresFixture
{
    protected override int MaximoDeRegistrosPorVentana => 2;
}

[Trait("Categoria", "Integracion")]
public class LimiteDePeticionesTests(ApiConLimiteEstrechoFixture api)
    : IClassFixture<ApiConLimiteEstrechoFixture>
{
    [Fact]
    public async Task SuperarElLimite_Responde429YDejaDeEnviarCorreos()
    {
        var cliente = api.CreateClient();

        for (var intento = 0; intento < 2; intento++)
        {
            var permitida = await cliente.PostAsJsonAsync("/registro/solicitudes",
                new PeticionDeSolicitudDeRegistro { Correo = $"permitido-{intento}@ejemplo.com" });

            Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
        }

        var correosAntes = api.Correo.EnlacesDeAlta.Count;

        var rechazada = await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = "rechazado@ejemplo.com" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);

        // Lo importante no es el código, sino que la petición rechazada no
        // llegue a generar un envío: el límite existe para no bombardear buzones.
        Assert.Equal(correosAntes, api.Correo.EnlacesDeAlta.Count);
    }

}

/// <summary>
/// Fixture propio: el cubo del limitador se comparte por origen y ventana, así
/// que dos tests dentro de la misma clase se estorbarían entre sí.
/// </summary>
public sealed class ApiConLimiteEstrechoParaContrasenaFixture : ApiConPostgresFixture
{
    protected override int MaximoDeRegistrosPorVentana => 2;
}

[Trait("Categoria", "Integracion")]
public class LimiteCompartidoConElRestablecimientoTests(ApiConLimiteEstrechoParaContrasenaFixture api)
    : IClassFixture<ApiConLimiteEstrechoParaContrasenaFixture>
{
    /// <summary>
    /// El alta y el restablecimiento comparten cubo a propósito: los dos mandan
    /// correo a una dirección que no es la de quien llama. Con cubos separados,
    /// un mismo origen duplicaría los envíos alternando entre ambos, y esta
    /// petición habría respondido 200.
    /// </summary>
    [Fact]
    public async Task AgotarElLimiteConElAlta_TambienBloqueaElRestablecimiento()
    {
        var cliente = api.CreateClient();

        for (var intento = 0; intento < 2; intento++)
        {
            var permitida = await cliente.PostAsJsonAsync("/registro/solicitudes",
                new PeticionDeSolicitudDeRegistro { Correo = $"compartido-{intento}@ejemplo.com" });

            Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
        }

        var correosAntes = api.Correo.EnlacesDeContrasena.Count;

        var rechazada = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = "compartido-0@ejemplo.com" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);
        Assert.Equal(correosAntes, api.Correo.EnlacesDeContrasena.Count);
    }
}
