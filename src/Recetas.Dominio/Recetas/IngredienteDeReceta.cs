namespace Recetas.Dominio.Recetas;

/// <summary>
/// El uso de un ingrediente dentro de una receta concreta: cuánto y en qué unidad.
/// </summary>
/// <remarks>
/// Separar esto de <see cref="Ingrediente"/> es lo que permite que "tomate" sea
/// una única fila compartida y, aun así, cada receta lleve su propia cantidad.
/// </remarks>
public sealed class IngredienteDeReceta
{
    private IngredienteDeReceta(Guid recetaId, Guid ingredienteId, decimal? cantidad, Unidad unidad)
    {
        RecetaId = recetaId;
        IngredienteId = ingredienteId;
        Cantidad = cantidad;
        Unidad = unidad;
    }

    /// <summary>Constructor para EF Core.</summary>
    private IngredienteDeReceta()
    {
    }

    public Guid RecetaId { get; private set; }

    public Guid IngredienteId { get; private set; }

    public Ingrediente? Ingrediente { get; private set; }

    /// <summary>
    /// Cantidad. Es <c>null</c> cuando la unidad es <see cref="Unidad.AlGusto"/>:
    /// "sal al gusto" no tiene número, y un 0 no significaría lo mismo.
    /// </summary>
    public decimal? Cantidad { get; private set; }

    public Unidad Unidad { get; private set; }

    public static IngredienteDeReceta Crear(Guid recetaId, Guid ingredienteId, decimal? cantidad, Unidad unidad)
    {
        // Coherencia entre cantidad y unidad. Se normaliza en lugar de rechazar:
        // que llegue un número junto a "al gusto" es una incoherencia del cliente,
        // no algo que deba impedir guardar la receta.
        if (unidad == Unidad.AlGusto)
        {
            cantidad = null;
        }
        else if (cantidad is null or <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cantidad),
                $"Una cantidad en '{unidad}' debe ser mayor que cero.");
        }

        return new IngredienteDeReceta(recetaId, ingredienteId, cantidad, unidad);
    }
}
