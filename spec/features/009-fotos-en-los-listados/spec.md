# 009 · Fotos en los listados

**Estado:** hecho

## Qué hace

El recetario y los resultados de búsqueda muestran una foto de cada receta.
Para no servir el archivo original —hasta 8 MB— en cada tarjeta, la API genera y
guarda una **miniatura** de cada foto y la sirve por un endpoint propio.

## Por qué

Hoy los listados son texto puro: el nombre, el tipo de plato y poco más. Las
fotos existen desde la 004 pero solo se ven al abrir la ficha, así que el
recetario se recorre leyendo en lugar de mirando, que es justo al revés de como
se busca una receta que ya conoces.

Las miniaturas no son un añadido opcional del cambio: son lo que lo hace
posible. Un listado de cincuenta recetas pintado con los originales serían
cientos de megabytes, y encima viajan en base64 —un 33 % más— porque el endpoint
exige autenticación y una etiqueta `<img>` no manda cabeceras.

## Criterios de aceptación

### Generar la miniatura

- [x] Al subir una foto se genera además su miniatura, y ambas quedan en disco.
- [x] La miniatura conserva la proporción del original: no se recorta ni se deforma.
- [x] Una foto más estrecha que el ancho de la miniatura **no se amplía**: se guarda tal cual.
- [x] La miniatura pesa mucho menos que el original. Hay un test que lo comprueba con una imagen grande de verdad, y que además afirma que la de partida era grande: si no, la comparación no diría nada.
- [x] La miniatura tampoco lleva metadatos: sale de la imagen ya limpia, no del archivo original.

### Fotos anteriores a esta feature

- [x] Una foto subida antes de la 009 no tiene miniatura en disco; pedirla la genera en ese momento y la deja guardada. **Comprobado también en el navegador**, sobre una receta cargada antes de esta feature.
- [x] La segunda petición de esa misma miniatura ya no la regenera.

### Servirla

- [x] `GET /recetas/{recetaId}/fotos/{fotoId}/miniatura` devuelve la miniatura.
- [x] Exige sesión, como todo lo demás: sin JWT, `401`.
- [x] Respeta las mismas reglas de visibilidad que la foto completa: el autor siempre, y cualquier usuario registrado si la receta está publicada.
- [x] Pedir la miniatura de una receta privada ajena responde `404`, no `403`. Hay además un test de que en ese caso **ni siquiera se genera**: la generación perezosa no puede ser una puerta trasera.
- [x] Borrar una foto borra también su miniatura, y borrar la receta entera tampoco deja miniaturas huérfanas.

### En los listados

- [x] `ResumenDeReceta` indica cuál es la foto que representa a la receta, o que no tiene ninguna.
- [x] El recetario muestra esa miniatura en cada tarjeta.
- [x] Los resultados de búsqueda, también.
- [x] Una receta sin fotos se ve entera y alineada con las demás: el hueco se pinta con el icono de la aplicación.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que comprueba que la miniatura de una receta privada ajena responde `404`.
- [x] Existe un test que comprueba que borrar la foto borra los dos archivos.

## Fuera de alcance

- **Elegir la portada.** La receta se representa con su foto más antigua. Que el
  autor pueda designar otra es otra pantalla y otro campo; entra al backlog.
- **La ficha sigue mostrando las fotos a tamaño completo.** Cambiarla a
  miniaturas exigiría una forma de abrir la foto grande, y un enlace normal no
  sirve: el endpoint pide cabecera de autorización. Es una feature de visor, no
  parte de esta.
- **Regenerar en lote las miniaturas de las fotos ya subidas.** Se generan la
  primera vez que se piden, que llega al mismo sitio sin script de migración.
- **Varios tamaños** (un tamaño por tipo de pantalla) ni formatos modernos
  negociados por `Accept`.
