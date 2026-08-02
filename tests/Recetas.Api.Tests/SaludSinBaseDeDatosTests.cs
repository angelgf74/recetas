using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Recetas.Contratos.Salud;

namespace Recetas.Api.Tests;

/// <summary>
/// Con la base de datos inalcanzable, la API debe degradarse de forma controlada:
/// un 503 con cuerpo, no una excepción sin capturar. No necesita Docker.
/// </summary>
public class SaludSinBaseDeDatosTests
{
    [Fact]
    public async Task Salud_Responde503_CuandoLaBaseDeDatosNoResponde()
    {
        using var api = new ApiSinBaseDeDatos();
        var cliente = api.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeSalud>();

        Assert.NotNull(cuerpo);
        Assert.Equal("degradado", cuerpo.Estado);
        Assert.False(cuerpo.BaseDeDatos);
    }

    private sealed class ApiSinBaseDeDatos : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder constructor)
        {
            // Puerto sin nada escuchando: la conexión falla de inmediato.
            constructor.UseSetting(
                "ConnectionStrings:Recetas",
                "Host=127.0.0.1;Port=1;Database=inexistente;Username=nadie;Password=nada;Timeout=1;Command Timeout=1");
        }
    }
}
