# 022 · Visor de fotos

**Estado:** hecha

## Qué hace

En la ficha, las fotos pasan a verse como **una tira de miniaturas**. Al pulsar
una, se abre a tamaño completo sobre la página, con forma de pasar a la
siguiente y a la anterior, y de cerrar.

Borrar una foto se hace **desde el visor**, mirándola, y no desde la tira.

## Por qué

**Hoy la ficha descarga todas las fotos a tamaño completo.** El endpoint de fotos
exige cabecera de autorización, así que una `<img src>` normal no vale: la web
las trae con el cliente autenticado y las incrusta como `data:`, que además
engorda un 33 % por el base64. Una receta con cinco fotos de 3 MB son 20 MB
antes de poder leer los ingredientes, y encima en el móvil, que es donde se
cocina.

Las miniaturas existen desde la 009 y las usan los listados. La ficha no podía
usarlas porque **entonces no habría forma de ver la foto entera**: un enlace a la
imagen tampoco lleva la cabecera. El visor es lo que faltaba, y es la razón por
la que el backlog lo pedía "antes de poder pasar la ficha a miniaturas".

## Criterios de aceptación

- [x] La ficha pinta las fotos como miniaturas y **no descarga los archivos completos** al cargarse.
- [x] Al pulsar una miniatura se abre la foto a tamaño completo.
- [x] Con varias fotos, se puede pasar a la **siguiente** y a la **anterior**; con una sola, esos controles no aparecen.
- [x] Se cierra con un botón, con la tecla **Escape** y pulsando fuera de la foto.
- [x] Las flechas del teclado pasan de foto.
- [x] El visor dice **qué foto de cuántas** se está viendo.
- [x] Se anuncia como diálogo a un lector de pantalla, y al abrirse el teclado actúa dentro de él.
- [x] El autor puede **borrar la foto que está viendo**, con la misma confirmación de siempre; al borrarla, el visor se cierra.
- [x] Quien no es el autor no ve la opción de borrar.
- [x] Si la foto completa no se puede cargar, el visor lo dice en lugar de quedarse en blanco — lo resuelve `FotoDeReceta`, que ya tenía ese estado.

**Lo que no se ha visto:** nada de esto se ha probado en un navegador. bUnit
pinta el componente y dispara eventos, así que responde a "¿hay botón?" y
"¿cambia de foto?", pero **no dice si la superposición se ve bien, si la foto
cabe en la pantalla, ni si Escape y el clic en el fondo funcionan de verdad**.
Los criterios de cerrar con teclado, cerrar pulsando fuera y las flechas están
implementados y sin comprobar en pantalla.

## Fuera de alcance

- **Ampliar (zoom) y desplazar la foto.** El archivo se ve entero y ajustado a la pantalla. El zoom del navegador sigue funcionando.
- **Gestos de deslizar en el móvil.** Los botones de anterior y siguiente valen en cualquier pantalla; el deslizamiento exige JavaScript y una capa de eventos táctiles que no paga lo que cuesta hoy.
- **Precargar la foto siguiente.** Se pediría un archivo grande que quizá nadie mire, que es justo lo que esta feature viene a evitar.
- **Reordenar las fotos o elegir la portada.** La portada sigue siendo la más antigua; cambiarlo es la entrada de backlog "elegir la foto de portada", que es un campo nuevo.
- **Descargar la foto a un archivo.** La exportación de datos (019) ya se lleva las fotos.
- **El visor en Android.** La paridad de la app es la 014.
