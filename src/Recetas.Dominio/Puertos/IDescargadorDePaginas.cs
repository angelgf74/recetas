namespace Recetas.Dominio.Puertos;

/// <summary>
/// Trae el HTML de una dirección pública.
/// </summary>
/// <remarks>
/// El dominio no sabe de sockets ni de redirecciones, pero sí impone <b>qué</b>
/// tiene que garantizar el adaptador: que la petición no pueda alcanzar la red
/// interna del servidor. Es una feature entera colgando de esa propiedad, y por eso
/// se escribe aquí y no solo en la implementación.
/// </remarks>
public interface IDescargadorDePaginas
{
    /// <summary>
    /// Devuelve el HTML de la página, o <c>null</c> si no se ha podido traer.
    /// </summary>
    /// <remarks>
    /// <b>Un solo <c>null</c> para todos los fallos</b>, a propósito: dirección
    /// interna, servidor que no responde, 404, contenido que no es HTML o
    /// demasiado grande comparten salida. Distinguirlos convertiría el endpoint en
    /// un escáner de la red del servidor manejado desde fuera.
    /// </remarks>
    Task<string?> DescargarAsync(Uri direccion, CancellationToken cancelacion = default);

    /// <summary>
    /// Descarga los bytes de una imagen, con las mismas garantías que
    /// <see cref="DescargarAsync"/>: la petición no puede alcanzar la red interna
    /// del servidor. Existe para la 027, que importa además la foto de la receta.
    /// </summary>
    /// <remarks>
    /// <b>No filtra por el <c>Content-Type</c> que declare el servidor de
    /// origen.</b> El formato se decide por los bytes, en la capa de aplicación,
    /// igual que con las fotos que sube el propio usuario — un servidor de
    /// terceros es al menos tan capaz de mentir en la cabecera como un cliente.
    /// </remarks>
    /// <param name="maximoDeBytes">
    /// Tope de la descarga. Lo decide quien llama —el mismo que rige las fotos
    /// que sube el propio usuario— y no este puerto.
    /// </param>
    Task<byte[]?> DescargarImagenAsync(Uri direccion, long maximoDeBytes, CancellationToken cancelacion = default);
}
