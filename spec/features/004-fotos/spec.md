# 004 · Fotos

**Estado:** implementado ✅

## Qué hace

Permite añadir fotos a una receta, consultarlas y borrarlas.

El archivo se guarda en el disco del servidor y la base de datos solo conserva la
referencia y los metadatos. Las fotos **se sirven por un endpoint autenticado**
que comprueba quién pide qué: heredan la visibilidad de su receta, así que la foto
de una receta privada solo la ve su autor.

Es una feature de API. La interfaz llega con la 007.

## Por qué

Una receta sin foto se consulta peor: al cocinar, la imagen dice de un vistazo lo
que el texto tarda un párrafo en explicar. `mission.md` las considera contenido,
no adorno.

Va antes que la publicación (005) a propósito: es más fácil acertar con el control
de acceso mientras todo es privado que añadirlo después, cuando ya hay recetas
compartidas y un error expone fotos ajenas.

## Criterios de aceptación

### Subir

- [x] `POST /recetas/{id}/fotos` con una imagen válida responde `201` y devuelve su identificador.
- [x] Subir a la receta de otro usuario responde `404` y no guarda nada.
- [x] Se aceptan JPEG, PNG y WebP.
- [x] Un archivo que **no sea** una imagen se rechaza con `400`, aunque venga declarado como imagen: el tipo se determina por el contenido, no por lo que diga el cliente.
- [x] Un archivo mayor que el límite configurado responde `413`.
- [x] Un archivo vacío responde `400`.
- [x] El nombre del archivo en disco lo genera el servidor; el que envía el cliente no se usa jamás para construir rutas.
- [x] Sin token, `401`.

### Consultar

- [x] `GET /recetas/{id}/fotos/{fotoId}` devuelve la imagen con su tipo de contenido correcto.
- [x] Pedir la foto de una receta ajena responde `404`, igual que si no existiera.
- [x] Pedir una foto inexistente responde `404`.
- [x] La ficha de la receta incluye los identificadores de sus fotos.
- [x] Sin token, `401`. **La carpeta de fotos no es accesible por URL directa.**

### Borrar

- [x] `DELETE /recetas/{id}/fotos/{fotoId}` responde `204` y borra el archivo del disco.
- [x] Borrar la foto de otro usuario responde `404` y no borra nada.
- [x] Borrar una receta borra también sus fotos: ni filas ni archivos huérfanos.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que falla si un usuario logra ver o borrar la foto de otro.
- [x] Existe un test que comprueba que un archivo que no es imagen se rechaza.
- [x] El binario no se guarda en PostgreSQL.

## Fuera de alcance

- **Miniaturas y escalado** — backlog. Aquí se sirve la imagen tal cual se subió.
- **Reordenar las fotos** de una receta.
- **Editar la imagen** (recortar, rotar).
- **Interfaz web** — feature 007.

## ⚠ Pendiente antes de la feature 005

**Las imágenes conservan sus metadatos EXIF, incluida la ubicación GPS.**

Mientras las recetas son privadas el riesgo es acotado: solo el autor ve sus
fotos. Pero en cuanto la 005 permita publicar, cualquier usuario podría descargar
la foto de una receta pública y leer **las coordenadas de la casa** de quien la
hizo, porque los móviles las incrustan por defecto.

Limpiar los metadatos exige decodificar y volver a codificar la imagen, y eso
supone una dependencia nueva (una biblioteca de imagen) que aquí no se añade sin
justificarla. Se deja anotado como **requisito de la 005**, no como mejora
opcional: publicar sin resolverlo sería filtrar la ubicación de los usuarios.
