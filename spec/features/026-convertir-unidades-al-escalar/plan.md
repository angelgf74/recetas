# 026 · Convertir unidades al escalar — Plan

## Enfoque

Método nuevo en vez de un parámetro booleano en `Escalar`. Es la misma
decisión que separó `RetirarPorModeracion` de `CambiarVisibilidadAsync` en la
020: dos operaciones que dejan de significar lo mismo no deben compartir
camino. `Escalar` (redondear en la misma unidad) sigue exactamente igual —
ningún test de la 010 se toca—; `EscalarConUnidad` es la nueva, y solo la usa
`Receta.EscalarA` cuando de verdad se ha pedido escalar.

```csharp
public static (decimal? Cantidad, Unidad Unidad) EscalarConUnidad(
    decimal? cantidad, Unidad unidad, decimal factor)
{
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);

    if (cantidad is not { } valor || !SeEscala(unidad))
    {
        return (cantidad, unidad);
    }

    var bruto = valor * factor;
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
```

La conversión pasa **antes** de redondear, no después: redondear primero en
gramos y volver a redondear en kilogramos acumularía dos redondeos donde
solo hace falta uno.

`Receta.EscalarA` decide qué método usar con la misma condición que ya
existe para saber si hay algo que escalar:

```csharp
public IReadOnlyList<LineaEscalada> EscalarA(int? racionesPedidas)
{
    var factor = FactorPara(racionesPedidas);
    var conversion = PuedeEscalarseA(racionesPedidas);

    return _ingredientes
        .Select(linea =>
        {
            var (cantidad, unidad) = conversion
                ? EscaladoDeCantidades.EscalarConUnidad(linea.Cantidad, linea.Unidad, factor)
                : (EscaladoDeCantidades.Escalar(linea.Cantidad, linea.Unidad, factor), linea.Unidad);

            return new LineaEscalada(linea.IngredienteId, linea.Ingrediente, cantidad, unidad);
        })
        .ToList();
}
```

## Nada más que tocar

`LineaEscalada.Unidad` ya es un campo propio, no heredado ciegamente de la
línea guardada — el comentario de su propio archivo ya avisa de que existe
aparte para no arriesgarse a persistir por error una cantidad escalada.
`RespuestaDeReceta.Ingredientes` y la web ya pintan `linea.Unidad` de cada
línea de la respuesta, así que la conversión llega a la ficha sin tocar la
API ni Blazor: `TextosDeEnumerado.DeUnidad`/`DeCantidad` ya saben mostrar
kilogramos y litros porque son valores del mismo enumerado de siempre.

## Archivos afectados

- `EscaladoDeCantidades.cs` — método nuevo, el existente intacto.
- `Receta.cs` — `EscalarA` decide cuál usar.
- Tests: `EscaladoDeCantidadesTests.cs` (nuevos, para `EscalarConUnidad`),
  `RecetaTests.cs` o `RacionesDeRecetaTests.cs` (que la vista sin escalar y el
  formulario de edición no conviertan), integración si hace falta comprobar
  el viaje completo por la API.
