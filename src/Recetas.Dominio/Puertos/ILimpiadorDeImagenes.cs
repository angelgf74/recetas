using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

/// <summary>
/// Deja una imagen sin metadatos, lista para guardarse.
/// </summary>
/// <remarks>
/// Existe por una razón concreta: los móviles incrustan en el EXIF la ubicación
/// GPS donde se hizo la foto. Publicar una receta con esa foto intacta expondría
/// la dirección de quien la cocinó, casi siempre su casa.
/// <para>
/// Se limpia al subir y no al publicar: hacerlo al publicar dejaría en disco
/// copias con ubicación mientras la receta es privada, y bastaría un fallo futuro
/// de permisos para exponerlas. Limpiando en la puerta, el dato sensible no llega
/// a existir en el servidor.
/// </para>
/// </remarks>
public interface ILimpiadorDeImagenes
{
    /// <summary>
    /// Devuelve la imagen sin metadatos, o <c>null</c> si el contenido no se puede
    /// interpretar como imagen del tipo indicado.
    /// </summary>
    /// <remarks>
    /// Debe respetar la orientación: el dato que dice si la foto se hizo en
    /// vertical vive precisamente en el EXIF que se va a descartar, así que hay
    /// que aplicarlo a los píxeles antes de tirarlo o las fotos se guardarían
    /// tumbadas.
    /// </remarks>
    Task<Stream?> LimpiarAsync(Stream original, TipoDeImagen tipo, CancellationToken cancelacion = default);
}
