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

    [Fact]
    public void LaLecturaDeReferencias_DevuelveNombresDeProyecto_NoRutas()
    {
        // Vigila la herramienta, no el código. Si esto se rompe, el resto de
        // tests de este archivo dejan de proteger nada **sin fallar**: son casi
        // todos `DoesNotContain`, y un valor mal extraído nunca coincide con lo
        // prohibido, así que pasan siempre.
        //
        // No es hipotético: en Linux ocurría exactamente eso, porque los .csproj
        // escriben las rutas con barras invertidas.
        var referencias = ProyectosReferenciadosPor("src/Recetas.Api/Recetas.Api.csproj");

        Assert.NotEmpty(referencias);

        Assert.All(referencias, referencia =>
        {
            Assert.DoesNotContain("\\", referencia);
            Assert.DoesNotContain("/", referencia);
            Assert.StartsWith("Recetas.", referencia);
        });
    }

    [Fact]
    public void Web_NoReferenciaAInfraestructuraNiALaApi()
    {
        // El cliente Blazor habla HTTP con la API y comparte los contratos. Si
        // acabara referenciando la infraestructura, se llevaría EF Core y las
        // credenciales al navegador del usuario.
        var referencias = ProyectosReferenciadosPor("src/Recetas.Web/Recetas.Web.csproj");

        Assert.DoesNotContain("Recetas.Infraestructura", referencias);
        Assert.DoesNotContain("Recetas.Api", referencias);
        Assert.DoesNotContain("Recetas.Dominio", referencias);
    }

    [Fact]
    public void LaWeb_EntraEnLaCadenaDeCompilacionDeLosTests()
    {
        // `dotnet test` solo construye los proyectos que alcanzan los de prueba.
        // Mientras ninguno referenciaba a Recetas.Web, sus errores de compilación
        // no aparecían hasta el despliegue: pasó en la 016, y en agosto de 2026 se
        // comprobó rompiéndola a propósito — 540 pruebas en verde y salida 0.
        //
        // Este test vigila que esa referencia siga existiendo. Da igual desde qué
        // proyecto de prueba: lo que importa es que alguien la construya.
        var proyectosDePrueba = Directory.GetFiles(
            Path.Combine(RaizDelRepositorio(), "tests"),
            "*.csproj",
            SearchOption.AllDirectories);

        var laReferencian = proyectosDePrueba
            .Where(ruta => ProyectosReferenciadosPor(ruta).Contains("Recetas.Web"))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        Assert.True(
            laReferencian.Length > 0,
            "Ningún proyecto de tests referencia Recetas.Web, así que `dotnet test` no la compila " +
            "y un error suyo no aparecerá hasta desplegar.");
    }

    private static string[] PaquetesDe(string rutaRelativa) =>
        DocumentoDe(rutaRelativa)
            .Descendants("PackageReference")
            .Select(elemento => elemento.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

    private static string[] ProyectosReferenciadosPor(string rutaRelativa) =>
        DocumentoDe(rutaRelativa)
            .Descendants("ProjectReference")
            .Select(elemento => NombreDelProyecto(elemento.Attribute("Include")?.Value ?? string.Empty))
            .ToArray();

    /// <summary>
    /// Extrae el nombre del proyecto de una ruta de <c>ProjectReference</c>.
    /// </summary>
    /// <remarks>
    /// <b>Las barras invertidas se normalizan primero, y no es cosmético.</b> Los
    /// <c>.csproj</c> escriben las rutas al estilo de Windows
    /// (<c>..\..\src\Recetas.Web\Recetas.Web.csproj</c>), y en Linux
    /// <c>Path.GetFileNameWithoutExtension</c> no reconoce <c>\</c> como
    /// separador: devolvía la ruta entera en lugar del nombre.
    /// <para>
    /// El efecto era peor que un test roto. Las comprobaciones de este archivo
    /// son en su mayoría <c>DoesNotContain</c>, así que en Linux <b>pasaban
    /// siempre</b>, hubiera violación de la regla de dependencias o no: el valor
    /// comparado nunca podía coincidir. Lo destapó la integración continua al
    /// ejecutarlas por primera vez fuera de Windows.
    /// </para>
    /// </remarks>
    private static string NombreDelProyecto(string include) =>
        Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));

    /// <summary>
    /// Admite ruta relativa a la raíz del repositorio o absoluta: <c>Path.Combine</c>
    /// devuelve la segunda tal cual si ya lo es.
    /// </summary>
    private static XDocument DocumentoDe(string ruta) =>
        XDocument.Load(Path.Combine(RaizDelRepositorio(), ruta));

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
