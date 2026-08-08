using Microsoft.Extensions.Logging;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Recetas.Infraestructura.Fotos;

/// <summary>
/// Quita los metadatos decodificando la imagen y volviéndola a codificar.
/// </summary>
/// <remarks>
/// Se recodifica entera en lugar de recortar el segmento EXIF. Recortar sería más
/// barato, pero solo cubre JPEG y deja intactos otros contenedores de metadatos
/// (XMP, IPTC) que también pueden llevar ubicación. Recodificar garantiza que no
/// queda nada, que es la propiedad que interesa cuando equivocarse significa
/// publicar la dirección de alguien.
/// </remarks>
public sealed class LimpiadorDeImagenesConImageSharp(ILogger<LimpiadorDeImagenesConImageSharp> registro)
    : ILimpiadorDeImagenes
{
    /// <summary>
    /// Calidad de recodificación del JPEG. Alta a propósito: la imagen se
    /// recodifica una sola vez, al subirla, y bajarla más se notaría en pantalla.
    /// </summary>
    private const int CalidadJpeg = 90;

    public async Task<Stream?> LimpiarAsync(
        Stream original,
        TipoDeImagen tipo,
        CancellationToken cancelacion = default)
    {
        try
        {
            using var imagen = await Image.LoadAsync(original, cancelacion);

            // La orientación vive en el EXIF que estamos a punto de tirar. Sin
            // aplicarla antes, una foto hecha en vertical se guardaría tumbada:
            // es el síntoma visible de limpiar metadatos a lo bruto.
            imagen.Mutate(contexto => contexto.AutoOrient());

            // Fuera todo: EXIF (con el GPS), XMP, IPTC y el perfil ICC.
            imagen.Metadata.ExifProfile = null;
            imagen.Metadata.XmpProfile = null;
            imagen.Metadata.IptcProfile = null;
            imagen.Metadata.IccProfile = null;

            var limpia = new MemoryStream();

            await imagen.SaveAsync(limpia, Codificador(tipo), cancelacion);

            limpia.Position = 0;
            return limpia;
        }
        catch (Exception excepcion) when (excepcion is UnknownImageFormatException or InvalidImageContentException)
        {
            // El detector de cabeceras ya filtró lo que no es imagen; llegar aquí
            // significa una imagen con cabecera válida pero contenido corrupto.
            registro.LogWarning(excepcion, "Se rechazó una imagen que no se ha podido decodificar.");
            return null;
        }
    }

    private static SixLabors.ImageSharp.Formats.IImageEncoder Codificador(TipoDeImagen tipo) => tipo switch
    {
        TipoDeImagen.Jpeg => new JpegEncoder { Quality = CalidadJpeg },
        TipoDeImagen.Png => new PngEncoder(),
        TipoDeImagen.Webp => new WebpEncoder(),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };
}
