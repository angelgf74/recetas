namespace Recetas.Dominio.Recetas;

/// <summary>
/// Una línea de ingrediente con la cantidad ya ajustada a otro número de raciones.
/// </summary>
/// <remarks>
/// Tipo aparte de <see cref="IngredienteDeReceta"/> a propósito: esto no es una
/// entidad, no se persiste y no tiene identidad. Reutilizar la entidad invitaría a
/// guardar por error una cantidad escalada como si fuera la de la receta.
/// </remarks>
public sealed record LineaEscalada(
    Guid IngredienteId,
    Ingrediente? Ingrediente,
    decimal? Cantidad,
    Unidad Unidad);
