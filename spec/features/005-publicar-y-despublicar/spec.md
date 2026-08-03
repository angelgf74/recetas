# 005 · Publicar y despublicar

**Estado:** implementado ✅

## Qué hace

Permite al autor **publicar** una receta y **despublicarla** cuando quiera. Una
receta publicada la pueden leer los demás usuarios registrados; una privada sigue
siendo solo del autor.

Incluye, como requisito previo, **limpiar los metadatos de las imágenes**: sin eso
publicar una receta filtraría la ubicación donde se hizo la foto.

Es una feature de API. La interfaz llega con la 007.

## Por qué

Es lo que da sentido a la distinción privada/pública que existe en el modelo desde
la 002. Hasta ahora el recetario es un cajón personal; publicar es lo que lo
convierte en algo que se comparte.

También es la condición para la 006: buscar solo tiene sentido cuando hay recetas
de otros que encontrar.

## Criterios de aceptación

### Limpieza de metadatos (requisito previo)

- [x] Una imagen subida con metadatos EXIF se guarda **sin ellos**.
- [x] En particular, se elimina la etiqueta de ubicación GPS.
- [x] La imagen guardada sigue siendo válida y se puede descargar y visualizar.
- [x] La orientación de la foto se respeta: una imagen hecha en vertical no se guarda tumbada.
- [x] Las fotos **ya subidas** antes de esta feature también quedan limpias. _No aplicó: la carpeta de producción estaba vacía, nunca se llegó a subir ninguna foto. No se construyó la herramienta de migración porque no tendría nada que migrar._

### Publicar

- [x] `POST /recetas/{id}/publicacion` cambia la visibilidad a pública y responde `204`.
- [x] `DELETE /recetas/{id}/publicacion` la devuelve a privada.
- [x] Publicar la receta de otro usuario responde `404`.
- [x] Publicar una receta ya pública no falla: el resultado es el mismo.
- [x] La ficha de la receta refleja la visibilidad actual.

### Leer recetas de otros

- [x] `GET /recetas/{id}` de una receta **pública de otro usuario** la devuelve.
- [x] `GET /recetas/{id}` de una receta **privada de otro usuario** sigue respondiendo `404`.
- [x] Un usuario no puede editar ni borrar una receta ajena aunque sea pública.
- [x] Las fotos de una receta pública las puede descargar cualquier usuario registrado.
- [x] Las fotos de una receta privada siguen siendo solo del autor.
- [x] Sin token, todo responde `401`: no hay lectura anónima ni siquiera de lo público.
- [x] `GET /recetas` sigue devolviendo **solo las propias**, públicas y privadas.
- [x] Una receta pública no expone el correo de su autor.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que falla si una receta privada ajena se vuelve legible.
- [x] Existe un test que falla si un usuario edita o borra una receta pública ajena.
- [x] Existe un test que comprueba que una imagen con GPS se guarda sin él.

## Fuera de alcance

- **Explorar o listar lo publicado.** Buscar es la 006; aquí solo se accede a una receta pública si se conoce su identificador.
- **Comentarios, valoraciones y seguidores.** `mission.md` los descarta explícitamente: público significa "consultable", no "conversable".
- **Interfaz web** — feature 007.
- **Avisar al autor** de que alguien ha visto su receta.
