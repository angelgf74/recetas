# 026 · Convertir unidades al escalar

**Estado:** hecha

## Qué hace

Al escalar una receta a otro número de raciones, si una cantidad en gramos o
mililitros llega o supera 1000, se muestra en kilogramos o litros. 500 g que
se doblan pasan a leerse "1 kg", no "1000 g".

## Por qué

La 010 resolvió multiplicar y redondear a algo medible. Le queda un caso
suelto: una cantidad puede seguir siendo "medible" en su unidad original y
aun así leerse peor de lo que leería en la unidad de al lado. "1000 g" es
correcto y es también el momento exacto en que cualquiera diría "un kilo".

## Criterios de aceptación

- [x] Una cantidad en gramos que al escalar llega o supera 1000 se muestra en kilogramos, ya redondeada a la unidad de kilogramo (cuartos).
- [x] Lo mismo para mililitros y litros.
- [x] Por debajo de 1000, sigue en gramos o mililitros: no hay conversión de vuelta en esta feature.
- [x] **Solo ocurre al pedir explícitamente un número de raciones.** La ficha sin tocar "Para N raciones" y el formulario de edición siguen viendo la unidad guardada tal cual — nunca kilogramos donde se guardaron gramos.
- [x] Lo guardado no cambia. Es una conversión de lectura, igual que el redondeo de la 010: la receta sigue teniendo gramos por dentro.
- [x] El resto de unidades (cucharada, cucharadita, taza, las contables) no convierten a nada: siguen redondeando como hasta ahora.

**No se ha visto en un navegador.** El comportamiento se apoya por completo
en `linea.Unidad` de la respuesta de la API, que la ficha ya pintaba desde
la 010; no hizo falta tocar la web, pero tampoco se ha comprobado en
pantalla que un "1,25 kg" se lea bien junto al resto de la ficha.

## Decisiones

**Solo al pedir raciones explícitamente, nunca en la vista por defecto ni en
la edición.** `RespuestaDeReceta.Ingredientes` sale del mismo método
(`Receta.EscalarA`) tanto para la ficha en reposo como para precargar el
formulario de edición, y los dos se piden sin `?raciones=`. Si la conversión
se aplicara siempre, editar una receta guardada en gramos rellenaría el
campo con kilogramos, y guardar sin tocar ese campo **cambiaría la unidad
guardada por un efecto secundario de mirarla**. Se gatea con la misma
condición que ya decide si hay algo que escalar (`PuedeEscalarseA`).

**Solo gramo→kilogramo y mililitro→litro.** Son los dos pares que ya
conviven como unidades separadas en el enumerado, con una equivalencia
exacta (1000:1) y sin ambigüedad. El resto —cucharadita a cucharada, taza a
litro— no tiene una razón de conversión tan clara y se queda fuera.

## Fuera de alcance

- **Convertir hacia abajo** (kilogramos a gramos cuando el resultado baja de
  1). Cubre el caso que nombra el backlog —doblar una receta— y evita
  duplicar la tabla de equivalencias y el riesgo de que un valor cerca del
  límite salte de unidad de un lado a otro entre vistas parecidas.
- **Otros pares de unidades** sin equivalencia exacta en el enumerado actual.
