using Microsoft.Extensions.Logging.Abstractions;
using Recetas.Dominio.Recetas;
using Recetas.Infraestructura.Fotos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Recetas.Infraestructura.Tests.Fotos;

/// <summary>
/// Comprueba el escalado con imágenes reales: proporción, tope y peso.
/// </summary>
public class EscaladorDeImagenesTests
{
    private readonly EscaladorDeImagenesConImageSharp _escalador =
        new(NullLogger<EscaladorDeImagenesConImageSharp>.Instance);

    /// <summary>
    /// Imagen con ruido, no un color plano: un lienzo liso se comprime tanto que
    /// cualquier medida de peso saldría trucada.
    /// </summary>
    private static MemoryStream Jpeg(int ancho, int alto)
    {
        using var imagen = new Image<Rgba32>(ancho, alto);
        var aleatorio = new Random(42);

        imagen.ProcessPixelRows(filas =>
        {
            for (var y = 0; y < filas.Height; y++)
            {
                var fila = filas.GetRowSpan(y);

                for (var x = 0; x < fila.Length; x++)
                {
                    fila[x] = new Rgba32(
                        (byte)aleatorio.Next(256),
                        (byte)aleatorio.Next(256),
                        (byte)aleatorio.Next(256));
                }
            }
        });

        var flujo = new MemoryStream();
        imagen.Save(flujo, new JpegEncoder { Quality = 90 });
        flujo.Position = 0;

        return flujo;
    }

    [Fact]
    public async Task Escalar_ReduceAlAnchoPedido()
    {
        using var grande = Jpeg(1600, 1200);

        using var reducida = await _escalador.EscalarAsync(grande, TipoDeImagen.Jpeg, 320);
        using var resultado = await Image.LoadAsync(reducida!);

        Assert.Equal(320, resultado.Width);
    }

    [Fact]
    public async Task Escalar_ConservaLaProporcion()
    {
        using var grande = Jpeg(1600, 1200);

        using var reducida = await _escalador.EscalarAsync(grande, TipoDeImagen.Jpeg, 320);
        using var resultado = await Image.LoadAsync(reducida!);

        // 4:3 sigue siendo 4:3. Si recortara a cuadrado, aquí saldría 320.
        Assert.Equal(240, resultado.Height);
    }

    [Fact]
    public async Task Escalar_ConUnaFotoVertical_TambienConservaLaProporcion()
    {
        using var vertical = Jpeg(600, 1200);

        using var reducida = await _escalador.EscalarAsync(vertical, TipoDeImagen.Jpeg, 320);
        using var resultado = await Image.LoadAsync(reducida!);

        Assert.Equal(320, resultado.Width);
        Assert.Equal(640, resultado.Height);
    }

    /// <summary>
    /// Ampliar una foto ya pequeña la haría pesar más y verse peor, que es lo
    /// contrario de lo que hace una miniatura.
    /// </summary>
    [Fact]
    public async Task Escalar_NoAmpliaLoQueYaEsMasPequeno()
    {
        using var pequena = Jpeg(120, 90);

        using var reducida = await _escalador.EscalarAsync(pequena, TipoDeImagen.Jpeg, 320);
        using var resultado = await Image.LoadAsync(reducida!);

        Assert.Equal(120, resultado.Width);
        Assert.Equal(90, resultado.Height);
    }

    /// <summary>El motivo entero de la feature: la miniatura tiene que pesar poco.</summary>
    [Fact]
    public async Task Escalar_ReduceMuchoElPeso()
    {
        using var grande = Jpeg(1600, 1200);
        var pesoOriginal = grande.Length;

        using var reducida = await _escalador.EscalarAsync(grande, TipoDeImagen.Jpeg, 320);

        // Se comprueba también el montaje: si la imagen de partida fuera diminuta,
        // la comparación de abajo no diría nada.
        Assert.True(pesoOriginal > 200_000, $"La imagen de partida solo pesa {pesoOriginal} bytes.");
        Assert.True(
            reducida!.Length < pesoOriginal / 10,
            $"La miniatura pesa {reducida.Length} y el original {pesoOriginal}.");
    }

    [Fact]
    public async Task Escalar_ConservaElFormato()
    {
        using var imagen = new Image<Rgba32>(800, 600);
        using var comoPng = new MemoryStream();
        await imagen.SaveAsync(comoPng, new PngEncoder());
        comoPng.Position = 0;

        using var reducida = await _escalador.EscalarAsync(comoPng, TipoDeImagen.Png, 320);
        var formato = await Image.DetectFormatAsync(reducida!);

        Assert.Equal(PngFormat.Instance, formato);
    }

    [Fact]
    public async Task Escalar_NoDejaMetadatosPropios()
    {
        using var grande = Jpeg(800, 600);

        using var reducida = await _escalador.EscalarAsync(grande, TipoDeImagen.Jpeg, 320);
        using var resultado = await Image.LoadAsync(reducida!);

        // La miniatura sale de la imagen ya limpia, pero conviene que el escalado
        // tampoco introduzca nada por su cuenta.
        Assert.True(
            resultado.Metadata.ExifProfile is null || resultado.Metadata.ExifProfile.Values.Count == 0,
            "La miniatura lleva metadatos EXIF.");
    }

    [Fact]
    public async Task Escalar_RechazaContenidoQueNoSePuedeDecodificar()
    {
        using var basura = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 1, 2, 3, 4, 5, 6, 7, 8]);

        var reducida = await _escalador.EscalarAsync(basura, TipoDeImagen.Jpeg, 320);

        Assert.Null(reducida);
    }
}
