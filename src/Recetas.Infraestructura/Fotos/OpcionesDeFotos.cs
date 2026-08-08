namespace Recetas.Infraestructura.Fotos;

/// <summary>Configuración del almacenamiento de fotos.</summary>
public sealed class OpcionesDeFotos
{
    public const string Seccion = "Fotos";

    /// <summary>
    /// Carpeta donde se guardan los archivos. En el servidor es
    /// <c>/apps/recetas/fotos</c>, declarada en la unidad de systemd como
    /// escribible pese a <c>ProtectSystem=strict</c>.
    /// </summary>
    public string Directorio { get; set; } = "fotos";

    /// <summary>
    /// Tamaño máximo por foto. Debe quedar por debajo del
    /// <c>client_max_body_size</c> de nginx (10 MB), o nginx cortaría la petición
    /// antes de que la API pudiera responder un error entendible.
    /// </summary>
    public long TamanoMaximoEnBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>
    /// Por debajo de este espacio libre, <c>GET /salud</c> responde degradado.
    /// </summary>
    /// <remarks>
    /// Configurable porque el mínimo razonable depende del servidor. El valor por
    /// defecto deja sitio para unas cuantas fotos al tamaño máximo permitido: el
    /// objetivo es enterarse <b>antes</b> de que un usuario se encuentre con que
    /// no puede subir la suya, no cuando ya no cabe ninguna.
    /// </remarks>
    public long MinimoDeEspacioLibreEnMb { get; set; } = 200;
}
