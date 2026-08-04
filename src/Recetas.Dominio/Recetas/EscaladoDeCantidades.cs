namespace Recetas.Dominio.Recetas;

/// <summary>
/// Ajusta la cantidad de un ingrediente a otro número de raciones.
/// </summary>
/// <remarks>
/// Multiplicar es lo fácil; lo que hace útil esta operación es <b>redondear a algo
/// que se pueda medir en una cocina</b>. "1,3333 huevos" o "0,4166 cucharaditas"
/// son peores que no escalar: obligan a hacer mentalmente el trabajo que la
/// función venía a evitar.
/// <para>
/// Por eso el redondeo depende de la unidad, y por eso vive en el dominio y no en
/// la web: es una regla de negocio, y cada superficie que la reimplementara
/// acabaría redondeando distinto.
/// </para>
/// </remarks>
public static class EscaladoDeCantidades
{
    /// <summary>
    /// Devuelve la cantidad ajustada por el factor, ya redondeada según su unidad.
    /// </summary>
    /// <remarks>
    /// Devuelve <c>null</c> cuando la línea no tiene cantidad, y la cantidad
    /// intacta cuando su unidad no admite escalado.
    /// </remarks>
    public static decimal? Escalar(decimal? cantidad, Unidad unidad, decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);

        if (cantidad is not { } valor || !SeEscala(unidad))
        {
            return cantidad;
        }

        var paso = PasoDeRedondeo(unidad);
        var escalada = Math.Round(valor * factor / paso, MidpointRounding.AwayFromZero) * paso;

        // Dividir una cucharadita entre ocho da 0,125, que redondeado a cuartos
        // sería 0 y borraría el ingrediente de la receta. Un poco de más es mejor
        // que hacerlo desaparecer.
        return escalada <= 0 ? paso : escalada;
    }

    /// <summary>
    /// Si multiplicar esa unidad significa algo.
    /// </summary>
    /// <remarks>
    /// <see cref="Unidad.AlGusto"/> no lleva número. <see cref="Unidad.Pizca"/> sí,
    /// pero "cuatro pizcas" no es una medida: es una pizca echada cuatro veces.
    /// Multiplicarla daría una precisión que no existe.
    /// </remarks>
    public static bool SeEscala(Unidad unidad) => unidad is not (Unidad.AlGusto or Unidad.Pizca);

    /// <summary>
    /// A qué múltiplo se redondea cada unidad.
    /// </summary>
    private static decimal PasoDeRedondeo(Unidad unidad) => unidad switch
    {
        // Contables. A media unidad: media cebolla o medio diente de ajo son
        // cantidades reales que cualquiera sabe preparar; 0,37 cebollas no.
        Unidad.Unidad or Unidad.Diente or Unidad.Rama or Unidad.Hoja => 0.5m,

        // Se miden con báscula o jarra, donde la fracción no aporta nada.
        Unidad.Gramo or Unidad.Mililitro => 1m,

        // Cucharas, tazas y las unidades grandes. A cuartos: un cuarto de
        // cucharadita se mide con una cuchara medidora normal.
        _ => 0.25m
    };
}
