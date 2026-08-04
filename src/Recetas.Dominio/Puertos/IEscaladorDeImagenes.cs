using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

/// <summary>
/// Reduce una imagen para servirla en un listado.
/// </summary>
/// <remarks>
/// Puerto aparte de <see cref="ILimpiadorDeImagenes"/> a propósito: limpiar
/// metadatos y redimensionar son dos cosas distintas, con dos motivos distintos
/// —uno es de privacidad y el otro de peso—, y juntarlas obligaría a cualquier
/// implementación futura a hacer las dos.
/// </remarks>
public interface IEscaladorDeImagenes
{
    /// <summary>Ancho al que se reducen las miniaturas.</summary>
    /// <remarks>
    /// Pequeño a propósito: cada miniatura viaja en base64 —un 33 % más— dentro de
    /// una respuesta por tarjeta, y un listado puede traer cincuenta.
    /// El doble del tamaño en el que se pinta, para que se vea nítida en pantallas
    /// de alta densidad.
    /// </remarks>
    public const int AnchoDeMiniatura = 320;

    /// <summary>
    /// Devuelve la imagen reducida a <paramref name="anchoMaximo"/>, o <c>null</c>
    /// si el contenido no se puede interpretar.
    /// </summary>
    /// <remarks>
    /// Conserva la proporción y <b>no amplía</b>: una imagen ya más estrecha que el
    /// máximo se devuelve tal cual. Ampliarla la haría pesar más y verse peor, que
    /// es justo lo contrario de lo que se busca.
    /// </remarks>
    Task<Stream?> EscalarAsync(
        Stream original,
        TipoDeImagen tipo,
        int anchoMaximo,
        CancellationToken cancelacion = default);
}
