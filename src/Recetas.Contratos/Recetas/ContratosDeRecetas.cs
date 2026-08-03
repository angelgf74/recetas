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
}

/// <param name="Nombre">Nombre normalizado del ingrediente.</param>
/// <param name="Cantidad">Cantidad, o <c>null</c> si es al gusto.</param>
/// <param name="Unidad">Unidad de medida.</param>
public sealed record LineaDeIngredienteRespuesta(string Nombre, decimal? Cantidad, string Unidad);

/// <summary>Receta completa, con sus ingredientes.</summary>
public sealed record RespuestaDeReceta(
    Guid Id,
    string Nombre,
    string TipoDePlato,
    string Elaboracion,
    string Visibilidad,
    DateTimeOffset FechaDeCreacion,
    DateTimeOffset FechaDeModificacion,
    IReadOnlyCollection<LineaDeIngredienteRespuesta> Ingredientes);

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
    DateTimeOffset FechaDeModificacion);
