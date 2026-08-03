namespace Recetas.Dominio.Recetas;

/// <summary>
/// Lista cerrada de tipos de plato.
/// </summary>
/// <remarks>
/// El criterio es <b>el momento del menú</b>, no la naturaleza del plato. Por eso
/// no hay `Ensalada`, `SopaOCrema` ni `Reposteria`: se solapaban con los momentos
/// (una crema es un primer plato, una tarta un postre) y obligaban al usuario a
/// elegir entre dos casillas igual de válidas, con el resultado de que la misma
/// receta acababa clasificada distinto según quién la subiera.
/// <para>
/// Añadir un valor implica cambio de código y migración. Se persiste como texto,
/// no como número: así una reordenación del enumerado no reescribe el significado
/// de los datos ya guardados.
/// </para>
/// </remarks>
public enum TipoDePlato
{
    Entrante,
    PrimerPlato,
    PlatoPrincipal,
    Guarnicion,
    Postre,
    Bebida,
    Salsa
}
