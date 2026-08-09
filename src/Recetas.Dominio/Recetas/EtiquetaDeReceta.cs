namespace Recetas.Dominio.Recetas;

/// <summary>
/// El vínculo entre una receta y una etiqueta del catálogo.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="IngredienteDeReceta"/>, no lleva datos propios:
/// una etiqueta no tiene cantidad ni unidad, así que esto es membresía pura.
/// Sigue siendo una entidad explícita y no un many-to-many implícito de EF por
/// la misma clave compuesta que usa <c>IngredienteDeReceta</c>: evita el
/// terreno de la trampa de EF documentada en <c>CLAUDE.md</c>, donde una clave
/// <c>Guid</c> puesta por el dominio y colgada de un padre ya rastreado puede
/// acabar generando un <c>UPDATE</c> donde tocaba un <c>INSERT</c>.
/// </remarks>
public sealed class EtiquetaDeReceta
{
    private EtiquetaDeReceta(Guid recetaId, Guid etiquetaId)
    {
        RecetaId = recetaId;
        EtiquetaId = etiquetaId;
    }

    /// <summary>Constructor para EF Core.</summary>
    private EtiquetaDeReceta()
    {
    }

    public Guid RecetaId { get; private set; }

    public Guid EtiquetaId { get; private set; }

    public Etiqueta? Etiqueta { get; private set; }

    public static EtiquetaDeReceta Crear(Guid recetaId, Guid etiquetaId) => new(recetaId, etiquetaId);
}
