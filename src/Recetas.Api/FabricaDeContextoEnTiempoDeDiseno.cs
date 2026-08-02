using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Recetas.Infraestructura.Persistencia;

namespace Recetas.Api;

/// <summary>
/// Construye el <see cref="RecetasDbContext"/> para las herramientas de EF
/// (<c>dotnet ef</c> y el bundle de migraciones).
/// </summary>
/// <remarks>
/// <para>
/// Sin esta fábrica, EF arranca el host completo de la aplicación para localizar
/// el contexto. Eso hace que migrar exija toda la configuración de la API,
/// incluida la clave de firma del JWT, que en el servidor vive en un fichero
/// legible solo por <c>webapps</c> y que el despliegue deliberadamente no lee.
/// El síntoma era un fallo de migración quejándose de <c>Jwt:ClaveDeFirma</c>,
/// que no tiene nada que ver con la base de datos.
/// </para>
/// <para>
/// Migrar solo necesita una cadena de conexión, y eso es lo único que se pide aquí.
/// </para>
/// </remarks>
public sealed class FabricaDeContextoEnTiempoDeDiseno : IDesignTimeDbContextFactory<RecetasDbContext>
{
    public RecetasDbContext CreateDbContext(string[] args)
    {
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        // En el servidor la cadena llega por variable de entorno y, además, el
        // bundle la sobrescribe con --connection. En local sale de
        // appsettings.Development.json, igual que al ejecutar la API.
        var cadenaDeConexion = configuracion.GetConnectionString("Recetas")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'Recetas'. Defínela en appsettings.Development.json, " +
                "en la variable de entorno ConnectionStrings__Recetas, o pásala con --connection.");

        var opciones = new DbContextOptionsBuilder<RecetasDbContext>()
            .UseNpgsql(cadenaDeConexion)
            .Options;

        return new RecetasDbContext(opciones);
    }
}
