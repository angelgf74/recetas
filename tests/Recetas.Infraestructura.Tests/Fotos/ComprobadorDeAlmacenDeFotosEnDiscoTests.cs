using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Recetas.Infraestructura.Fotos;

namespace Recetas.Infraestructura.Tests.Fotos;

/// <summary>
/// Diagnóstico del disco de fotos, contra archivos de verdad.
/// </summary>
public class ComprobadorDeAlmacenDeFotosEnDiscoTests : IDisposable
{
    private readonly string _directorio =
        Path.Combine(Path.GetTempPath(), $"recetas-salud-{Guid.NewGuid():N}");

    public ComprobadorDeAlmacenDeFotosEnDiscoTests() => Directory.CreateDirectory(_directorio);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directorio, recursive: true);
        }
        catch (IOException)
        {
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Acepta_CuandoElDirectorioExisteYHaySitio()
    {
        var comprobador = Crear(_directorio, minimoEnMb: 1);

        Assert.True(await comprobador.AceptaFotosAsync());
    }

    [Fact]
    public async Task NoAcepta_SiElDirectorioNoExiste()
    {
        var comprobador = Crear(Path.Combine(_directorio, "no-existe"), minimoEnMb: 1);

        Assert.False(await comprobador.AceptaFotosAsync());
    }

    [Fact]
    public async Task NoAcepta_SiElEspacioLibreEstaPorDebajoDelUmbral()
    {
        // Umbral imposible: ningún disco tiene un exabyte libre. Es la forma de
        // probar el camino de "no queda sitio" sin llenar un disco de verdad.
        var comprobador = Crear(_directorio, minimoEnMb: 1024L * 1024 * 1024 * 1024);

        Assert.False(await comprobador.AceptaFotosAsync());
    }

    [Fact]
    public async Task Comprobar_NoDejaArchivosDetras()
    {
        // Una sonda que deja basura acaba llenando justo lo que vigila, y se
        // llama cada pocos minutos durante años.
        var comprobador = Crear(_directorio, minimoEnMb: 1);

        for (var i = 0; i < 5; i++)
        {
            Assert.True(await comprobador.AceptaFotosAsync());
        }

        Assert.Empty(Directory.GetFileSystemEntries(_directorio));
    }

    [Fact]
    public async Task Comprobar_EscribeDeVerdad_NoSoloMiraPermisos()
    {
        // Se sustituye el directorio por un archivo con ese mismo nombre: los
        // permisos dirían que todo bien, pero crear algo dentro es imposible.
        // Es lo que distingue "he mirado los permisos" de "he escrito".
        var ruta = Path.Combine(_directorio, "ocupado");
        await File.WriteAllTextAsync(ruta, "no soy un directorio");

        var comprobador = Crear(ruta, minimoEnMb: 1);

        Assert.False(await comprobador.AceptaFotosAsync());
    }

    private static ComprobadorDeAlmacenDeFotosEnDisco Crear(string directorio, long minimoEnMb) =>
        new(Options.Create(new OpcionesDeFotos
        {
            Directorio = directorio,
            MinimoDeEspacioLibreEnMb = minimoEnMb
        }),
        NullLogger<ComprobadorDeAlmacenDeFotosEnDisco>.Instance);
}
