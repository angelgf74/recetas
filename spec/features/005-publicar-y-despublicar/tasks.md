# 005 · Publicar y despublicar — Tareas

## Paso 0 · Limpieza de metadatos (requisito previo)

- [x] Añadir `SixLabors.ImageSharp` a `Recetas.Infraestructura`.
- [x] Puerto `ILimpiadorDeImagenes` en el dominio.
- [x] `LimpiadorDeImagenesConImageSharp`: recodifica sin metadatos y aplica la orientación.
- [x] `GestionDeFotos.SubirAsync` limpia antes de guardar.
- [x] Limpiar las fotos ya subidas. _No hacía falta: la carpeta de producción estaba vacía (ver nota)._

## Paso 1 · Dominio

- [x] `Receta.PuedeVerla(usuarioId)`.
- [x] `Receta.Publicar` y `Receta.Despublicar`, idempotentes.

## Paso 2 · Aplicación

- [x] `ObtenerAsync` de recetas pasa a usar `PuedeVerla`.
- [x] `CambiarVisibilidadAsync`, que sigue exigiendo autoría.
- [x] `GestionDeFotos.ObtenerAsync` pasa a usar `PuedeVerla`; subir y borrar no.

## Paso 3 · API

- [x] `POST` y `DELETE` sobre `/recetas/{id}/publicacion`.
- [x] Repasar que la respuesta no exponga datos del autor. _Nunca los ha incluido: `RespuestaDeReceta` no tiene ningún campo de autor._

## Validación

- [x] Test de que una imagen con GPS se guarda sin metadatos.
- [x] Test de que una foto vertical no se guarda tumbada.
- [x] Tests de dominio de `PuedeVerla`, `Publicar` y `Despublicar`.
- [x] Test de que una receta pública ajena se lee.
- [x] Test de que una receta privada ajena sigue dando `404`.
- [x] Test de que **no** se puede editar ni borrar una receta pública ajena.
- [x] Test de que la foto de una privada ajena sigue dando `404`.
- [x] Test de que sin token todo sigue dando `401`.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **No se construyó la herramienta para limpiar fotos antiguas.** El plan la daba por necesaria, pero al mirar el servidor la carpeta `/apps/recetas/fotos` estaba **vacía**: la feature 004 se desplegó pero nadie llegó a subir ninguna foto, porque en producción el correo de alta lo envía Brevo de verdad y no se pudo crear una cuenta de prueba por API. Sin fotos antiguas no hay nada que migrar.
- **Publicar y despublicar comparten un solo caso de uso** (`CambiarVisibilidadAsync` con un `bool`) en lugar de dos. La lógica es idéntica salvo el estado destino, y duplicarla habría duplicado también la comprobación de autoría.

## Nota: ImageSharp 4 no compila sin licencia

Al añadir el paquete sin fijar versión entró la **4.0.0**, y la compilación falló:

```
error : No Six Labors license found. Set $(SixLaborsLicenseKey), set
$(SixLaborsLicenseFile), or add a 'sixlabors.lic' file to the project/workspace.
```

La v4 exige clave de licencia **en tiempo de compilación**, no solo en las
condiciones de uso. Se fijó la **3.1.12**, que usa la Six Labors Split License
(gratuita para código abierto y organizaciones pequeñas) y no pide clave.

Conviene saberlo antes de actualizar dependencias sin mirar: pasar de la 3 a la 4
rompe la compilación, no solo las condiciones legales.

## Nota: los tests usaban imágenes falsas

Los tests de fotos de la 004 inventaban los bytes de cabecera, y bastaba porque el
servidor solo los olfateaba. Al empezar a **decodificar** para limpiar metadatos,
esas imágenes falsas pasaron a rechazarse —correctamente— y cuatro tests se
pusieron en rojo.

Se añadió `ImagenDePrueba`, que genera imágenes minúsculas pero reales. También
hubo que cambiar una aserción que comparaba los bytes descargados con los subidos:
ya no coinciden, y **debe ser así**, porque el servidor recodifica.
