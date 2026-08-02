using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Recetas.Dominio.Puertos;
using Recetas.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace Recetas.Api.Tests;

/// <summary>
/// Levanta un PostgreSQL efímero con Testcontainers y arranca la API apuntando a él.
/// Cada ejecución parte de una base limpia: los tests no heredan el estado de la anterior.
/// </summary>
public class ApiConPostgresFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("recetas_test")
        .WithUsername("recetas")
        .WithPassword("recetas_test")
        .Build();

    /// <summary>Correos que la API ha intentado enviar. Es donde se lee el enlace del alta.</summary>
    public EnviadorDeCorreoEspia Correo { get; } = new();

    /// <summary>Permite a un test bajar el límite de registros para comprobar el 429.</summary>
    protected virtual int MaximoDeRegistrosPorVentana => 1000;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var ambito = Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<RecetasDbContext>();
        await contexto.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseSetting("ConnectionStrings:Recetas", _postgres.GetConnectionString());

        // Clave solo de test. La real vive fuera del repositorio.
        constructor.UseSetting(
            "Jwt:ClaveDeFirma",
            "clave-de-pruebas-suficientemente-larga-para-hmac-sha256");

        constructor.UseSetting("Correo:BaseDeLaWeb", "https://recetas.test");
        constructor.UseSetting("Limites:RegistrosPorVentana", MaximoDeRegistrosPorVentana.ToString());

        constructor.ConfigureTestServices(servicios =>
        {
            // Sustituye el enviador por un espía: ningún test manda correo real,
            // y es la única forma de leer el token que viaja en el enlace.
            servicios.RemoveAll<IEnviadorDeCorreo>();
            servicios.AddSingleton<IEnviadorDeCorreo>(Correo);
        });
    }
}
