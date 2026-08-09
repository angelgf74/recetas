namespace Recetas.Dominio.Recetas;

/// <summary>
/// Criterios de búsqueda, todos opcionales y combinables.
/// </summary>
/// <param name="Nombre">Texto que debe contener el nombre. Ya normalizado.</param>
/// <param name="Ingredientes">
/// Ingredientes que debe llevar la receta, ya normalizados. Se combinan con
/// <b>Y</b>: la receta tiene que llevarlos todos. Con O, cuantos más ingredientes
/// se indicaran más resultados saldrían, que es lo contrario de lo que espera
/// quien está afinando una búsqueda.
/// </param>
/// <param name="TipoDePlato">Tipo de plato exacto, si se indica.</param>
/// <param name="Etiquetas">
/// Etiquetas que debe llevar la receta, ya normalizadas. Se combinan con
/// <b>Y</b>, igual que <paramref name="Ingredientes"/> y por el mismo motivo.
/// </param>
public sealed record CriteriosDeBusqueda(
    string? Nombre,
    IReadOnlyCollection<string> Ingredientes,
    TipoDePlato? TipoDePlato,
    IReadOnlyCollection<string> Etiquetas)
{
    /// <summary>Construye los criterios normalizando el texto tal como llega del usuario.</summary>
    public static CriteriosDeBusqueda Crear(
        string? nombre,
        IEnumerable<string>? ingredientes,
        TipoDePlato? tipoDePlato,
        IEnumerable<string>? etiquetas = null)
    {
        // La consulta pasa por la MISMA normalización que el texto guardado. Si
        // cada lado usara reglas distintas no casarían nunca, y el fallo sería
        // silencioso: búsquedas vacías sin ningún error.
        var nombreNormalizado = TextoParaBusqueda.Normalizar(nombre);

        var ingredientesNormalizados = (ingredientes ?? [])
            .Select(TextoParaBusqueda.Normalizar)
            .Where(valor => valor.Length > 0)
            .Distinct()
            .ToList();

        var etiquetasNormalizadas = (etiquetas ?? [])
            .Select(TextoParaBusqueda.Normalizar)
            .Where(valor => valor.Length > 0)
            .Distinct()
            .ToList();

        return new CriteriosDeBusqueda(
            nombreNormalizado.Length > 0 ? nombreNormalizado : null,
            ingredientesNormalizados,
            tipoDePlato,
            etiquetasNormalizadas);
    }
}
