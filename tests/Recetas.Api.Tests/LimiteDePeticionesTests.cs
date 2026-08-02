using System.Net;
using System.Net.Http.Json;
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
