using System.Xml.Linq;

namespace Recetas.Arquitectura.Tests;

/// <summary>
/// Complemento de <see cref="ReglaDeDependenciasTests"/>, que inspecciona el ensamblado compilado.
/// <para>
/// Ese enfoque tiene un punto ciego comprobado: si alguien añade el paquete de EF Core a
/// <c>Recetas.Dominio</c> pero todavía no usa ningún tipo suyo, el compilador no emite la
/// referencia en el manifiesto y la violación pasa desapercibida hasta el primer uso real.
/// </para>
/// <para>
/// Estos tests leen directamente los <c>.csproj</c>, así que detectan la infracción en el momento
/// en que se declara la dependencia, no cuando se estrena.
/// </para>
/// </summary>
public class ReferenciasDeProyectoTests
{
    private static readonly string[] PaquetesProhibidosEnElDominio =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "Microsoft.Extensions.DependencyInjection"
    ];

    [Fact]
    public void Dominio_NoDeclaraPaquetesDeInfraestructura()
    {
        var prohibidos = PaquetesDe("src/Recetas.Dominio/Recetas.Dominio.csproj")
            .Where(paquete => PaquetesProhibidosEnElDominio.Any(prohibido =>
                paquete.StartsWith(prohibido, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            prohibidos.Length == 0,
            $"Recetas.Dominio no puede declarar paquetes de infraestructura: {string.Join(", ", prohibidos)}.");
    }

    [Fact]
    public void Dominio_NoDeclaraReferenciasAOtrosProyectos()
    {
        var referencias = ProyectosReferenciadosPor("src/Recetas.Dominio/Recetas.Dominio.csproj");

        Assert.True(
            referencias.Length == 0,
            $"Recetas.Dominio es el centro: no referencia a nadie. Encontrado: {string.Join(", ", referencias)}.");
    }

    [Fact]
    public void Aplicacion_NoReferenciaAInfraestructura()
    {
        var referencias = ProyectosReferenciadosPor("src/Recetas.Aplicacion/Recetas.Aplicacion.csproj");

        Assert.DoesNotContain("Recetas.Infraestructura", referencias);
        Assert.DoesNotContain("Recetas.Api", referencias);
    }

    [Fact]
    public void Contratos_NoReferenciaANadie()
    {
        var referencias = ProyectosReferenciadosPor("src/Recetas.Contratos/Recetas.Contratos.csproj");

        Assert.True(
            referencias.Length == 0,
            $"Recetas.Contratos debe poder publicarse solo. Encontrado: {string.Join(", ", referencias)}.");
    }

    private static string[] PaquetesDe(string rutaRelativa) =>
        DocumentoDe(rutaRelativa)
            .Descendants("PackageReference")
            .Select(elemento => elemento.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

    private static string[] ProyectosReferenciadosPor(string rutaRelativa) =>
        DocumentoDe(rutaRelativa)
            .Descendants("ProjectReference")
            .Select(elemento => Path.GetFileNameWithoutExtension(
                elemento.Attribute("Include")?.Value ?? string.Empty))
            .ToArray();

    private static XDocument DocumentoDe(string rutaRelativa) =>
        XDocument.Load(Path.Combine(RaizDelRepositorio(), rutaRelativa));

    /// <summary>
    /// Sube desde el directorio de salida hasta encontrar el archivo de solución.
    /// Acepta <c>.slnx</c> (formato de .NET 10) y <c>.sln</c>, para no romperse si se migra el formato.
    /// </summary>
    private static string RaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !ContieneLaSolucion(directorio))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
            ?? throw new InvalidOperationException(
                "No se encontró Recetas.slnx ni Recetas.sln subiendo desde el directorio de salida de los tests.");
    }

    private static bool ContieneLaSolucion(DirectoryInfo directorio) =>
        File.Exists(Path.Combine(directorio.FullName, "Recetas.slnx"))
        || File.Exists(Path.Combine(directorio.FullName, "Recetas.sln"));
}
