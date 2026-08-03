# 004 · Fotos — Tareas

## Dominio

- [x] Enumerado `TipoDeImagen` con tipo de contenido y extensión.
- [x] Entidad `Foto`.
- [x] `Receta` con colección de fotos, `AnadirFoto` y `QuitarFoto`.
- [x] Puerto `IAlmacenDeFotos`.

## Aplicación

- [x] `DetectorDeImagen`, por bytes de cabecera.
- [x] `GestionDeFotos` con subir, obtener y borrar.
- [x] Borrar receta borra también los archivos.

## Infraestructura

- [x] `AlmacenDeFotosEnDisco` y `OpcionesDeFotos`.
- [x] Configuración EF de `Foto` con cascada.
- [x] Migración `Fotos`.

## API

- [x] Los tres endpoints, autenticados.
- [x] Identificadores de fotos en la ficha de receta.
- [x] Límite de tamaño que responde `413`.

## Validación

- [x] Tests de detección de tipo, incluido un archivo que no es imagen.
- [x] Tests de aplicación de las tres operaciones y sus errores.
- [x] Test de que un usuario no ve ni borra la foto de otro.
- [x] Test de integración del ciclo completo contra PostgreSQL y disco real.
- [x] Test de que borrar la receta no deja archivos huérfanos.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Despliegue

- [x] `Fotos:Directorio` configurado en la unidad de systemd (`/apps/recetas/fotos`).
- [x] `ReadWritePaths` de esa carpeta, ya no opcional: sin ella `ProtectSystem=strict` haría fallar cada subida.
- [x] Documentado en `deploy/README.md` que la copia de seguridad son **dos piezas**.

## Nota: EF hacía UPDATE en vez de INSERT

Subir una foto respondía `500` con
`DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`,
un mensaje que no menciona la causa por ningún lado. En el registro se veía la
sentencia real: `UPDATE fotos SET …` cuando debía ser un `INSERT`.

El motivo es que el identificador lo genera el dominio (`Guid.NewGuid()`) y EF,
por omisión, considera que una clave `Guid` la genera el almacén al insertar. Al
encontrar una foto nueva colgando de una receta **ya rastreada**, ve la clave
rellena y deduce que la fila existe.

Al resto de entidades no les ocurre porque se añaden con `Add()` y EF marca el
grafo entero como nuevo; la foto es la primera que se cuelga de un padre que ya
existía. Se resuelve con `ValueGeneratedNever()` en la configuración de `Foto`.

## ⚠ Deuda que bloquea la feature 005

Las imágenes **conservan sus metadatos EXIF, incluida la ubicación GPS**. Hoy es
un riesgo acotado porque toda receta es privada, pero publicar sin limpiarlos
expondría las coordenadas de la casa de cada usuario. Ver el detalle en `spec.md`.
