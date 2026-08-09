using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Recetas;

/// <summary>
/// Línea de ingrediente en una petición.
/// </summary>
/// <param name="Nombre">Nombre tal como lo escribe el usuario; el servidor lo normaliza.</param>
/// <param name="Cantidad">Cantidad. Se ignora si la unidad es <c>AlGusto</c>.</param>
/// <param name="Unidad">Unidad de medida, de la lista cerrada.</param>
public sealed record LineaDeIngredientePeticion(
    [property: Required(ErrorMessage = "Cada ingrediente necesita un nombre.")]
    [property: MaxLength(80)]
    string Nombre,
    decimal? Cantidad,
    [property: Required] string Unidad);

/// <summary>Cuerpo de creación y de actualización de una receta.</summary>
public sealed class PeticionDeReceta
{
    [Required(ErrorMessage = "La receta necesita un nombre.")]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Uno de los valores de <c>TipoDePlato</c>. Se recibe como texto y no como
    /// número para que el contrato no dependa del orden del enumerado.
    /// </summary>
    [Required(ErrorMessage = "Indica el tipo de plato.")]
    public string TipoDePlato { get; set; } = string.Empty;

    [Required(ErrorMessage = "La receta necesita una elaboración.")]
    [MaxLength(20_000)]
    public string Elaboracion { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "La receta necesita al menos un ingrediente.")]
    public List<LineaDeIngredientePeticion> Ingredientes { get; set; } = [];

    /// <summary>
    /// Para cuántas raciones son las cantidades. Opcional: las recetas anteriores a
    /// la feature 010 no lo dicen, y sin ese dato simplemente no se puede escalar.
    /// </summary>
    [Range(RacionesMinimas, RacionesMaximas,
        ErrorMessage = "Las raciones deben estar entre {1} y {2}.")]
    public int? Raciones { get; set; }

    /// <summary>Debe coincidir con <c>Receta</c> en el dominio; se repite porque Contratos no lo referencia.</summary>
    public const int RacionesMinimas = 1;

    public const int RacionesMaximas = 100;

    /// <summary>
    /// Palabras libres, tal como las escribe el usuario; el servidor las
    /// normaliza. Complemento de <see cref="TipoDePlato"/>, no obligatorio.
    /// </summary>
    [MaxLength(MaximoDeEtiquetas, ErrorMessage = "Como mucho {1} etiquetas.")]
    public List<string> Etiquetas { get; set; } = [];

    /// <summary>Debe coincidir con <c>Receta.MaximoDeEtiquetas</c> en el dominio.</summary>
    public const int MaximoDeEtiquetas = 10;
}

/// <param name="Nombre">Nombre normalizado del ingrediente.</param>
/// <param name="Cantidad">Cantidad, o <c>null</c> si es al gusto.</param>
/// <param name="Unidad">Unidad de medida.</param>
public sealed record LineaDeIngredienteRespuesta(string Nombre, decimal? Cantidad, string Unidad);

/// <param name="Id">Identificador de la foto; con él se compone la URL de descarga.</param>
/// <param name="Tipo">Formato: <c>Jpeg</c>, <c>Png</c> o <c>Webp</c>.</param>
/// <param name="TamanoEnBytes">Tamaño del archivo.</param>
/// <param name="EsPortada">
/// Si es la que representa a la receta en el recetario, la búsqueda y los
/// favoritos (025). Nunca hay más de una <c>true</c> por receta.
/// </param>
public sealed record FotoRespuesta(Guid Id, string Tipo, long TamanoEnBytes, bool EsPortada = false);

/// <summary>Receta completa, con sus ingredientes y sus fotos.</summary>
public sealed record RespuestaDeReceta(
    Guid Id,
    string Nombre,
    string TipoDePlato,
    string Elaboracion,
    string Visibilidad,
    DateTimeOffset FechaDeCreacion,
    DateTimeOffset FechaDeModificacion,
    IReadOnlyCollection<LineaDeIngredienteRespuesta> Ingredientes,
    IReadOnlyCollection<FotoRespuesta> Fotos,
    /// <summary>
    /// Si la receta es del usuario que pregunta, igual que en el resumen.
    /// <para>
    /// Desde la 005 la ficha también sirve recetas ajenas publicadas, así que el
    /// cliente necesita saberlo para decidir qué acciones ofrece. Sin este campo
    /// habría que deducirlo pidiendo además el recetario propio y buscando la
    /// receta dentro: dos llamadas para pintar una pantalla, que es la señal de
    /// que faltaba un dato en la respuesta.
    /// </para>
    /// </summary>
    bool EsMia,
    /// <summary>
    /// Raciones que declara la receta, o <c>null</c> si no lo dice. Es el punto de
    /// partida del escalado: sin él, la ficha no puede ofrecerlo.
    /// </summary>
    int? Raciones = null,
    /// <summary>
    /// Para cuántas raciones son las cantidades <b>que van en esta respuesta</b>.
    /// </summary>
    /// <remarks>
    /// Se manda además de <see cref="Raciones"/> para que el cliente pueda decir
    /// "estás viendo 6 raciones de una receta para 4" sin tener que acordarse de lo
    /// que pidió, y para que sepa que lo que lee no son las cantidades guardadas.
    /// </remarks>
    int? RacionesMostradas = null,
    /// <summary>
    /// Si quien pregunta puede <b>retirar esta receta de la parte pública</b> sin ser
    /// su autor: solo el responsable del servicio, y solo sobre recetas publicadas.
    /// </summary>
    /// <remarks>
    /// Lo dice el servidor por el mismo motivo que <see cref="EsMia"/>: el cliente
    /// no puede deducirlo. Quién modera es configuración del servidor, y un cliente
    /// que corre en el navegador del usuario no debe conocerla ni decidir sobre ella.
    /// <para>
    /// Es solo para pintar la interfaz. La autorización de verdad la vuelve a hacer
    /// el servidor al recibir la petición.
    /// </para>
    /// </remarks>
    bool PuedoRetirarla = false,
    /// <summary>
    /// Si <b>quien pregunta</b> tiene esta receta en sus favoritos.
    /// </summary>
    /// <remarks>
    /// Es un dato de quien mira, no de la receta: el mismo identificador devuelve
    /// <c>true</c> a uno y <c>false</c> a otro. Por eso va aquí y no hay ningún
    /// campo que diga cuántas veces se ha marcado ni quién lo hizo — ese hueco es
    /// deliberado, y `mission.md` explica por qué: un recuento haría competir a las
    /// recetas entre sí, que es lo que las valoraciones descartadas hacían.
    /// </remarks>
    bool EsFavorita = false,
    /// <summary>
    /// Palabras libres, tal como están en el catálogo (ya en minúsculas, igual
    /// que los nombres de ingrediente). Puede venir vacía.
    /// </summary>
    /// <remarks>
    /// No entra en <see cref="ResumenDeReceta"/>: el listado no las necesita, y
    /// añadirlas ahí costaría una consulta más por cada receta del listado.
    /// Mismo argumento que llevó a dejar <c>EsFavorita</c> fuera del resumen.
    /// </remarks>
    IReadOnlyCollection<string>? Etiquetas = null);

/// <summary>
/// Receta en un listado: sin ingredientes ni elaboración.
/// </summary>
/// <remarks>
/// Devolver la receta entera en el listado obligaría a leer todas las líneas de
/// todas las recetas para pintar una lista que no las muestra.
/// </remarks>
public sealed record ResumenDeReceta(
    Guid Id,
    string Nombre,
    string TipoDePlato,
    string Visibilidad,
    DateTimeOffset FechaDeModificacion,
    /// <summary>
    /// Si la receta es del usuario que pregunta. La búsqueda mezcla las propias
    /// con las publicadas por otros, y la interfaz necesita distinguirlas para
    /// saber si puede ofrecer editar.
    /// <para>
    /// Se dice si es tuya, pero nunca de quién es la ajena: eso no aporta nada y
    /// expondría datos de otro usuario.
    /// </para>
    /// </summary>
    bool EsMia,
    /// <summary>
    /// Foto que representa a la receta en el listado, o <c>null</c> si no tiene
    /// ninguna. Con este identificador se compone la URL de la miniatura.
    /// </summary>
    /// <remarks>
    /// Va el identificador y no la lista entera de fotos: el listado pinta una
    /// sola, y devolver todas obligaría a leerlas para no usarlas.
    /// </remarks>
    Guid? FotoDePortadaId);

/// <param name="Resultados">Recetas encontradas, ya acotadas al tope.</param>
/// <param name="HayMas">
/// Si se han recortado resultados. Permite avisar al usuario de que afine la
/// búsqueda en lugar de dejarle creer que eso es todo lo que hay.
/// </param>
public sealed record RespuestaDeBusqueda(
    IReadOnlyCollection<ResumenDeReceta> Resultados,
    bool HayMas);
