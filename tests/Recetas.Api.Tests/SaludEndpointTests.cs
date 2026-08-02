using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Salud;

namespace Recetas.Api.Tests;

/// <summary>
/// Recorre el camino completo: petición HTTP, caso de uso, adaptador de EF y PostgreSQL real.
/// Requiere Docker. Marcado como <c>Integracion</c> para poder excluirlo sin él:
/// <c>dotnet test --filter "Categoria!=Integracion"</c>.
/// </summary>
[Trait("Categoria", "Integracion")]
public class SaludEndpointTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    [Fact]
    public async Task Salud_Responde200_CuandoLaBaseDeDatosEstaDisponible()
    {
        var cliente = api.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeSalud>();

        Assert.NotNull(cuerpo);
        Assert.Equal("correcto", cuerpo.Estado);
        Assert.True(cuerpo.BaseDeDatos);
    }
}
