namespace Recetas.Dominio.Recetas;

/// <summary>
/// Unidades de medida de un ingrediente dentro de una receta.
/// </summary>
/// <remarks>
/// Lista cerrada por el mismo motivo que <see cref="TipoDePlato"/>: con texto
/// libre acabarían conviviendo "cucharada", "cucharadas", "cda" y "c/s" como
/// cosas distintas, y cualquier agrupación o conversión futura sería imposible.
/// <para>
/// <see cref="AlGusto"/> existe porque "sal al gusto" es una línea de ingrediente
/// legítima y sin cantidad. Es lo que permite que <c>Cantidad</c> sea opcional en
/// lugar de obligar a inventarse un cero que no significaría nada.
/// </para>
/// </remarks>
public enum Unidad
{
    /// <summary>Sin cantidad: "sal al gusto", "pimienta al gusto".</summary>
    AlGusto,

    /// <summary>Piezas contables: "2 huevos", "1 cebolla".</summary>
    Unidad,

    Gramo,
    Kilogramo,
    Mililitro,
    Litro,
    Cucharada,
    Cucharadita,
    Taza,
    Pizca,

    /// <summary>Para ajo y similares.</summary>
    Diente,

    /// <summary>Para hierbas: "una rama de romero".</summary>
    Rama,

    /// <summary>Para laurel, albahaca…</summary>
    Hoja
}
