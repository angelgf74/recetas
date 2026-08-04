# 010 · Escalar cantidades — Plan

## Enfoque

Dos piezas independientes:

1. **`Receta.Raciones`**, entero opcional. Migración con columna nula: las recetas
   que ya existen no tienen ese dato y nadie puede inventárselo.
2. **El escalado**, que es una **lectura**. No hay endpoint nuevo ni verbo nuevo:
   la ficha admite `?raciones=N` y devuelve las cantidades ajustadas. Nada se
   escribe.

## Dónde vive el cálculo

**En el dominio, y el escalado se pide al servidor.**

La tentación es multiplicar en el navegador: el dato ya está ahí y el ajuste
sería instantáneo. Pero lo que hace útil esta feature no es multiplicar —eso lo
hace cualquiera— sino **redondear a algo que se pueda medir en una cocina**, y
esas reglas son de negocio. Ponerlas en Blazor las dejaría fuera del alcance de
Android, que tendría que reimplementarlas y divergir; y choca de frente con
`tech-stack.md`, que dice que la web no lleva reglas de negocio.

El precio es una petición por cada cambio del número de comensales. Es asumible:
la ficha ya se pide por HTTP y la respuesta es pequeña.

## Implementación

### Dominio

1. `Receta.Raciones` (`int?`), con `RacionesMinimas = 1` y `RacionesMaximas = 100`. Se fija al crear y al actualizar.
2. `EscaladoDeCantidades` — función pura: dada una cantidad, su unidad y un factor, devuelve la cantidad ajustada y redondeada. Es el único sitio donde vive la tabla de redondeos.
3. `Receta.EscalarA(raciones)` — devuelve las líneas ajustadas. **No muta la receta**: devuelve una proyección.

### Aplicación

4. `GestionDeRecetas.ObtenerAsync` gana el número de raciones pedido y devuelve, junto a la receta, las líneas ya escaladas.

### Infraestructura

5. Configuración EF de `Raciones` y migración `Raciones`.

### API

6. `GET /recetas/{id}` acepta `?raciones=N`, valida el rango y responde `400` fuera de él.
7. `RespuestaDeReceta` gana `Raciones` (las de la receta) y `RacionesMostradas` (para cuántas son las cantidades que van en esta respuesta).
8. `PeticionDeReceta` gana `Raciones`.

### Web

9. `FormularioDeReceta`: campo de raciones.
10. `FichaDeReceta`: control para cambiar comensales, aviso cuando lo mostrado no son las cantidades originales y forma de volver.

## Decisiones

- **Raciones opcional, no con un valor por defecto.** Poner "4" a las recetas que ya existen sería inventarse un dato: nadie ha dicho para cuántos son. Sin raciones, la ficha no ofrece el ajuste, que es honesto.
- **Escalar es leer, no editar.** Por eso va como parámetro de `GET` y no como endpoint aparte. Guardar la versión escalada sería otra cosa, y está fuera de alcance.
- **`AlGusto` y `Pizca` no escalan.** La primera no tiene número. La segunda lo tiene pero no significa nada: "cuatro pizcas" no es una medida, es una pizca cuatro veces. Multiplicarla daría una falsa precisión.
- **Redondeo por unidad, no uno solo para todo.** Es el corazón de la feature: "1,333 huevos" es peor que no escalar. Contables a media unidad, gramos y mililitros a entero, el resto a cuartos.
- **Nunca cero.** Dividir entre cuatro una cucharadita da 0,25; entre ocho, 0,125, que redondeado a cuartos sería 0 y borraría el ingrediente de la receta. Se sube al mínimo de su escala.
- **Se devuelven las dos cifras** —las raciones de la receta y las de lo mostrado— para que el cliente pueda decir "estás viendo 6 raciones de una receta para 4" sin tener que recordar lo que pidió.

## Riesgos

- **Una petición por cada cambio de comensales.** Ver arriba: aceptado a cambio de no duplicar las reglas de redondeo en cada superficie.
- **Redondear acumula error.** Escalar de 4 a 6 y de ahí a 9 no da lo mismo que de 4 a 9 directamente. Se evita escalando **siempre desde la receta guardada**, nunca desde lo ya escalado.
- **La elaboración menciona cantidades en texto.** "Añade los 200 g de harina" seguirá diciendo 200 aunque la lista diga 300. No hay forma fiable de tocar el texto libre; el aviso de que se está viendo una versión ajustada es lo que lo hace visible.
