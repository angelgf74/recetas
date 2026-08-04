namespace Recetas.Infraestructura.Importacion;

/// <summary>Límites de la descarga de páginas para importar recetas.</summary>
public sealed class OpcionesDeImportacion
{
    public const string Seccion = "Importacion";

    /// <summary>
    /// Tiempo máximo de la petición entera, redirecciones incluidas.
    /// </summary>
    /// <remarks>
    /// Sin tope, un servidor que acepta la conexión y no responde deja una petición
    /// de la API colgada indefinidamente. Corto a propósito: el usuario está
    /// esperando delante del formulario.
    /// </remarks>
    public int SegundosDeEspera { get; set; } = 10;

    /// <summary>
    /// Tope de bytes que se leen de la respuesta.
    /// </summary>
    /// <remarks>
    /// La cabecera <c>Content-Length</c> la escribe el servidor de destino y puede
    /// mentir, así que además se corta la lectura del flujo al llegar aquí.
    /// </remarks>
    public int MaximoDeBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>
    /// Saltos de redirección permitidos.
    /// </summary>
    /// <remarks>
    /// Se siguen a mano para poder comprobar cada destino: en automático, el primer
    /// salto podría ser público y el segundo apuntar a <c>127.0.0.1</c>.
    /// </remarks>
    public int MaximoDeRedirecciones { get; set; } = 5;
}
