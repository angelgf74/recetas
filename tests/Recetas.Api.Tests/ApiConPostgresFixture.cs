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

    /// <summary>
    /// Correo de la cuenta que puede moderar. Un test que quiera retirar contenido
    /// ajeno debe registrar un usuario con este correo exacto: la API reconoce al
    /// responsable comparando el del token con el de la configuración.
    /// </summary>
    public const string CorreoDelResponsable = "responsable@ejemplo.com";

    /// <summary>Carpeta temporal donde esta instancia guarda las fotos.</summary>
    public string DirectorioDeFotos { get; } =
        Path.Combine(Path.GetTempPath(), $"recetas-fotos-{Guid.NewGuid():N}");

    /// <summary>Permite a un test bajar el límite de registros para comprobar el 429.</summary>
    protected virtual int MaximoDeRegistrosPorVentana => 1000;

    /// <summary>
    /// Los tests comparten origen, así que todos caen en el mismo cubo del
    /// limitador. Con el valor de producción (10 por ventana), una clase con una
    /// docena de inicios de sesión empieza a recibir 429 a mitad, y el síntoma es
    /// un token nulo mucho más adelante, sin mencionar el límite por ningún lado.
    /// </summary>
    protected virtual int MaximoDeIniciosDeSesionPorVentana => 1000;

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

        // Se limpian los archivos escritos durante los tests. Si falla, no se
        // propaga: dejar basura en el directorio temporal no debe tumbar la suite.
        try
        {
            if (Directory.Exists(DirectorioDeFotos))
            {
                Directory.Delete(DirectorioDeFotos, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseSetting("ConnectionStrings:Recetas", _postgres.GetConnectionString());

        // Clave solo de test. La real vive fuera del repositorio.
        constructor.UseSetting(
            "Jwt:ClaveDeFirma",
            "clave-de-pruebas-suficientemente-larga-para-hmac-sha256");

        constructor.UseSetting("Correo:BaseDeLaWeb", "https://recetas.test");

        // Carpeta de fotos propia y efímera por fixture: los tests escriben
        // archivos de verdad y no deben pisarse entre sí ni ensuciar el repositorio.
        constructor.UseSetting("Fotos:Directorio", DirectorioDeFotos);
        constructor.UseSetting("Limites:RegistrosPorVentana", MaximoDeRegistrosPorVentana.ToString());
        constructor.UseSetting(
            "Limites:IniciosDeSesionPorVentana",
            MaximoDeIniciosDeSesionPorVentana.ToString());

        // Holgado: la ventana real de denuncias es de una hora, así que con el
        // valor de producción los tests que encadenan varias chocarían con el 429.
        constructor.UseSetting("Limites:DenunciasPorVentana", "500");

        // Y la de exportaciones también, que en producción son tres por hora.
        constructor.UseSetting("Limites:ExportacionesPorVentana", "500");

        // Quién modera. Un test que quiera retirar contenido ajeno registra un
        // usuario con exactamente este correo.
        constructor.UseSetting("Moderacion:CorreoDelResponsable", CorreoDelResponsable);

        constructor.ConfigureTestServices(servicios =>
        {
            // Sustituye el enviador por un espía: ningún test manda correo real,
            // y es la única forma de leer el token que viaja en el enlace.
            servicios.RemoveAll<IEnviadorDeCorreo>();
            servicios.AddSingleton<IEnviadorDeCorreo>(Correo);
        });
    }
}
