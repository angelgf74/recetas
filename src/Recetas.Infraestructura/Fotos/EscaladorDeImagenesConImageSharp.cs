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
/// Reduce la imagen con ImageSharp, conservando su formato.
/// </summary>
/// <remarks>
/// Conservar el formato reutiliza <see cref="TipoDeImagen"/> entero —extensión,
/// tipo de contenido y codificador— y evita tener que decidir contra qué fondo se
/// aplana la transparencia de un PNG. Un JPEG único pesaría menos; si llega a
/// molestar, es un cambio en <c>Codificador</c>.
/// </remarks>
public sealed class EscaladorDeImagenesConImageSharp(ILogger<EscaladorDeImagenesConImageSharp> registro)
    : IEscaladorDeImagenes
{
    /// <summary>
    /// Calidad del JPEG de la miniatura. Más baja que la del original (90): a este
    /// tamaño el artefacto no se aprecia y el ahorro sí, porque la miniatura viaja
    /// en base64 y se piden muchas de golpe.
    /// </summary>
    private const int CalidadJpeg = 75;

    public async Task<Stream?> EscalarAsync(
        Stream original,
        TipoDeImagen tipo,
        int anchoMaximo,
        CancellationToken cancelacion = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(anchoMaximo);

        try
        {
            using var imagen = await Image.LoadAsync(original, cancelacion);

            if (imagen.Width > anchoMaximo)
            {
                imagen.Mutate(contexto => contexto.Resize(new ResizeOptions
                {
                    // Solo se fija el ancho; el alto en 0 le dice a ImageSharp que
                    // lo calcule para conservar la proporción.
                    Size = new Size(anchoMaximo, 0),

                    // Max, no Crop: recortar decidiría por el usuario qué parte de
                    // su plato importa, y en un emplatado no suele ser el centro.
                    Mode = ResizeMode.Max
                }));
            }

            // Si la imagen ya era más estrecha no se toca: ampliarla la haría pesar
            // más y verse peor. Se recodifica igualmente, para que la miniatura de
            // un JPEG grande y la de uno pequeño salgan con la misma calidad.

            var reducida = new MemoryStream();

            await imagen.SaveAsync(reducida, Codificador(tipo), cancelacion);

            reducida.Position = 0;
            return reducida;
        }
        catch (Exception excepcion) when (excepcion is UnknownImageFormatException or InvalidImageContentException)
        {
            registro.LogWarning(excepcion, "No se ha podido escalar una imagen: contenido ilegible.");
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
