# 010 · Escalar cantidades por comensales

**Estado:** hecho

## Qué hace

Una receta puede decir **para cuántas raciones es**. En su ficha aparece un
control para cambiar ese número, y las cantidades de los ingredientes se ajustan.

La receta guardada **no cambia**: ajustar es una forma de leerla, no una edición.

## Por qué

Es lo que se hace de verdad en la cocina: la receta es para cuatro y hoy sois
dos, o al revés. Hoy toca dividir mentalmente doce cantidades mientras se cocina,
que es justo el momento en que peor se calcula.

## Criterios de aceptación

### Raciones de la receta

- [x] Al crear o editar una receta se pueden indicar sus raciones.
- [x] Es **opcional**: las recetas que ya existen no tienen raciones y siguen funcionando. Editar puede además quitarlas.
- [x] Una receta sin raciones no ofrece el ajuste: no hay desde cuántas escalar.
- [x] Las raciones son un entero entre 1 y 100. Fuera de ahí, `400`. Hay un test de arquitectura que vigila que el rango no se separe entre dominio y contratos.

### Escalar

- [x] `GET /recetas/{id}?raciones=N` devuelve la receta con las cantidades ajustadas.
- [x] Sin el parámetro, la receta llega tal cual está guardada.
- [x] La respuesta dice para cuántas raciones son las cantidades que trae, y cuántas tiene la receta original.
- [x] Pedir el mismo número de raciones que tiene la receta devuelve exactamente las cantidades guardadas.
- [x] Pedir raciones sobre una receta que no las tiene se ignora: llega sin escalar y sin error.
- [x] Escalar **no modifica la receta**: volver a pedirla sin el parámetro devuelve los valores originales. **Comprobado también contra la base de datos local**, tras escalar desde el navegador.
- [x] `raciones` fuera del rango 1–100 responde `400`.

### Qué escala y qué no

- [x] Un ingrediente "al gusto" sigue al gusto: no tiene cantidad que multiplicar.
- [x] Una "pizca" sigue siendo una pizca: multiplicarla no significa nada en la cocina.
- [x] Las unidades contables (unidades, dientes, ramas, hojas) se redondean a media unidad, y nunca a cero: media cebolla es una cantidad real, "0 cebollas" no.
- [x] Gramos y mililitros se redondean a entero.
- [x] El resto de unidades se redondean a un cuarto: un cuarto de cucharadita es medible, 0,3333 no.
- [x] Ninguna cantidad escalada se queda en cero.

### Web

- [x] La ficha de una receta con raciones muestra un control para cambiar el número.
- [x] Al cambiarlo, la lista de ingredientes se actualiza. **Probado en el navegador**: de 4 a 6 y de 4 a 3.
- [x] Se ve claramente cuándo lo que se está leyendo **no** son las cantidades originales, y se puede volver a ellas.
- [x] El formulario de crear y editar tiene el campo de raciones. **Probado en el navegador**: editar una receta de 4 a 8 raciones y comprobar contra la API que se guardó.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test de que escalar y volver a pedir la receta no ha cambiado nada en la base de datos.
- [x] Existe un test por cada regla de redondeo.

## Fuera de alcance

- **Convertir unidades** (pasar 1000 g a 1 kg cuando se dobla). Es otro problema:
  exige una tabla de equivalencias y decidir cuándo conviene cambiar de unidad.
- **Escalar los tiempos o las temperaturas** de la elaboración. El texto es libre
  y adivinar números dentro sería tan frágil como inútil: doblar la cantidad no
  dobla el tiempo de horno.
- **Guardar la versión escalada** como receta nueva.
