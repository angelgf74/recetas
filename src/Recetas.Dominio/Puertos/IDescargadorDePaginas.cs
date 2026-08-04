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
}
