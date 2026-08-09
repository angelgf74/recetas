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
    /// Como <see cref="Escalar"/>, pero además decide si conviene cambiar de
    /// unidad —de gramos a kilogramos, de mililitros a litros— cuando el
    /// resultado llega a 1000.
    /// </summary>
    /// <remarks>
    /// Método aparte y no un parámetro más en <see cref="Escalar"/> a propósito:
    /// la unidad guardada de una receta no debe cambiar por el simple hecho de
    /// mirarla. Solo <c>Receta.EscalarA</c> llama a este método, y solo cuando de
    /// verdad se ha pedido escalar — nunca al leer la ficha en reposo ni al
    /// precargar el formulario de edición, que comparten el mismo camino que la
    /// vista sin escalar.
    /// <para>
    /// La conversión ocurre <b>antes</b> de redondear, no después: redondear
    /// primero en gramos y volver a redondear en kilogramos acumularía dos
    /// redondeos donde solo hace falta uno.
    /// </para>
    /// </remarks>
    public static (decimal? Cantidad, Unidad Unidad) EscalarConUnidad(
        decimal? cantidad, Unidad unidad, decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);

        if (cantidad is not { } valor || !SeEscala(unidad))
        {
            return (cantidad, unidad);
        }

        var bruto = valor * factor;

        // Solo hacia arriba, y solo estos dos pares: son los únicos con una
        // equivalencia exacta (1000:1) entre dos unidades que ya conviven en el
        // enumerado. Convertir hacia abajo queda fuera a propósito (026).
        var (unidadFinal, divisor) = unidad switch
        {
            Unidad.Gramo when bruto >= 1000m => (Unidad.Kilogramo, 1000m),
            Unidad.Mililitro when bruto >= 1000m => (Unidad.Litro, 1000m),
            _ => (unidad, 1m)
        };

        var paso = PasoDeRedondeo(unidadFinal);
        var redondeada = Math.Round(bruto / divisor / paso, MidpointRounding.AwayFromZero) * paso;

        return (redondeada <= 0 ? paso : redondeada, unidadFinal);
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
