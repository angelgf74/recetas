# 024 · Etiquetas libres

**Estado:** hecha

## Qué hace

Poner a una receta palabras libres —"ensalada", "sin gluten", "rápido", "de la
abuela"— y buscar por ellas. Complemento de `TipoDePlato`, no sustituto: el
tipo sigue siendo obligatorio y de lista cerrada; las etiquetas son opcionales
y las inventa quien escribe.

## Por qué

`TipoDePlato` clasifica por **el momento del menú** a propósito, y
deliberadamente no cubre nada más: no hay `Ensalada` ni `SinGluten` porque se
solaparían con los momentos y obligarían a elegir entre dos casillas igual de
válidas. Eso deja fuera ejes reales por los que alguien querría encontrar una
receta —dieta, tiempo, ocasión, procedencia— y ninguno de ellos cabe en una
lista cerrada sin que el proyecto tenga que anticiparlos todos.

Las etiquetas resuelven eso sin tocar el enumerado: quien sube la receta pone
las palabras que le sirven a él, y esas mismas palabras sirven para
encontrarla después.

## Criterios de aceptación

- [x] Al crear o editar una receta, se pueden indicar **cero o más** etiquetas.
- [x] Dos etiquetas que solo difieren en mayúsculas o espacios son **la misma**: "Rápido", "rápido " y "rápido" no producen tres filas de catálogo.
- [x] Repetir una etiqueta en la misma receta no la duplica.
- [x] Hay un tope de etiquetas por receta (10). Superarlo responde igual que cualquier otro dato inválido de la receta (`400`).
- [x] La ficha muestra las etiquetas de la receta.
- [x] Se puede buscar por etiqueta, combinable con nombre, ingredientes y tipo de plato.
- [x] Buscar por varias etiquetas exige que la receta las lleve **todas**, igual que la búsqueda por ingredientes.
- [x] Una etiqueta que nadie más ha usado se crea sola al guardar la receta.
- [x] Etiquetas privadas ajenas no se filtran: buscar no puede servir para averiguar qué palabras usa alguien en recetas que no puedes ver, porque la búsqueda ya solo mira lo que puedes ver.
- [x] Quitar todas las etiquetas de una receta al editarla es válido: no son obligatorias.

**No se ha visto en un navegador.** El campo de etiquetas del formulario, su
visualización en la ficha y el campo de búsqueda están escritos y compilan,
pero no tienen test de bUnit ni se han abierto en un dispositivo.

## Fuera de alcance

- **Autocompletar con las etiquetas ya existentes en el catálogo.** Útil, pero es una llamada nueva y una interfaz de sugerencias; la entrada por texto libre separado por comas basta para empezar.
- **Etiquetas por categorías o jerarquías** ("dieta > sin gluten"). El backlog las pide sueltas.
- **Ver cuántas recetas usan una etiqueta**, en ningún sitio del producto ni de la API. No hace falta para el caso de uso y sería el mismo problema que los favoritos (021) resolvieron evitando: convertir una marca personal en algo comparable.
- **Etiquetas en Android.** Como el resto de escritura de recetas, es cosa de la web hasta que la 014 alcance paridad.
