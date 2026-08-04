# 009 · Fotos en los listados — Tareas

## Dominio

- [x] Puerto `IEscaladorDeImagenes`.
- [x] `IAlmacenDeFotos`: guardar, abrir y borrar la miniatura.
- [x] `Receta.FotoDePortada`, derivada de la foto más antigua.

## Aplicación

- [x] `SubirAsync` genera la miniatura desde la imagen ya limpia.
- [x] `ObtenerMiniaturaAsync`, con generación perezosa si falta.

## Infraestructura

- [x] `EscaladorDeImagenesConImageSharp`.
- [x] Rutas de la miniatura en `AlmacenDeFotosEnDisco`, y borrado de las dos.
- [x] Cargar las fotos en el recetario y en la búsqueda (`Include`), o la portada saldría siempre vacía.

## API

- [x] `GET /recetas/{recetaId}/fotos/{fotoId}/miniatura`.
- [x] `FotoDePortadaId` en `ResumenDeReceta`.

## Web

- [x] `FotoDeReceta` sabe pedir la miniatura.
- [x] Portada en el recetario y en la búsqueda, con hueco cuando no hay foto.

## Validación

- [x] Test de que la miniatura conserva la proporción y no amplía.
- [x] Test de que la miniatura pesa mucho menos que el original.
- [x] Test de que la miniatura de una receta privada ajena responde `404`.
- [x] Test de que una receta publicada sí deja descargarla a otro usuario.
- [x] Test de la generación perezosa: sin archivo, se crea; a la segunda, ya está.
- [x] Test de que borrar la foto borra los dos archivos.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
