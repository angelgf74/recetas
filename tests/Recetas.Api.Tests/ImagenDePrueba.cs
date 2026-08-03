using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Recetas.Api.Tests;

/// <summary>
/// Genera imágenes reales para los tests.
/// </summary>
/// <remarks>
/// Antes bastaba con inventarse los bytes de cabecera, porque la subida solo los
/// olfateaba. Desde que la feature 005 limpia los metadatos, el servidor
/// <b>decodifica</b> la imagen: unos bytes falsos se rechazan, con razón. Estas
/// imágenes son minúsculas pero válidas de verdad.
/// </remarks>
public static class ImagenDePrueba
{
    public static byte[] Jpeg(int ancho = 8, int alto = 8) => Generar(ancho, alto, new JpegEncoder());

    public static byte[] Png(int ancho = 8, int alto = 8) => Generar(ancho, alto, new PngEncoder());

    private static byte[] Generar(int ancho, int alto, SixLabors.ImageSharp.Formats.IImageEncoder codificador)
    {
        using var imagen = new Image<Rgba32>(ancho, alto);
        using var flujo = new MemoryStream();

        imagen.Save(flujo, codificador);

        return flujo.ToArray();
    }
}
