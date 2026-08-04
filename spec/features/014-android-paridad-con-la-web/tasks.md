# 014 · Android: paridad con la web — Tareas

## Datos

- [x] Contratos de escritura, espejo de `Recetas.Contratos`.
- [x] `ClienteDeApi`: crear, actualizar, borrar, publicar, despublicar, fotos, importar y receta con raciones.

## Modelo

- [x] Acciones y pantallas nuevas en `AppViewModel`.

## Interfaz

- [x] Formulario de receta, el mismo para crear y editar.
- [x] Acciones en la ficha: editar, foto, compartir, borrar.
- [x] Todas las fotos en la ficha, con borrado por foto.
- [x] Ajuste de comensales.
- [x] Importar desde URL dentro del formulario.
- [x] Alta y recuperación de contraseña desde la aplicación.
- [x] Confirmación antes de borrar.

## Enlaces de aplicación

- [x] Filtro de intenciones en el manifiesto, con `autoVerify`.
- [x] `assetlinks.json` publicado en la web con la huella de **depuración**.
- [x] El token del enlace abre la pantalla de elegir contraseña.
- [ ] Añadir la huella del certificado de **publicación** cuando exista. Sin eso, los enlaces no abrirán la aplicación descargada de Google Play.

## Validación

- [x] `gradlew assembleDebug` y `gradlew test` en verde, sin avisos.
- [x] Probado en un Xiaomi MI 8 Lite contra producción: crear una receta, verla en su ficha con todas las acciones, y borrarla con confirmación.
- [x] Comprobado que la ficha sigue sin anuncios y que el recetario sí los lleva.
- [ ] Probar en el dispositivo: editar, subir y borrar foto, publicar, escalar, importar, alta y recuperación. **Ver abajo.**
- [ ] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Lo que no se ha probado a mano

Se comprobó el ciclo crear → ver → borrar de punta a punta en un dispositivo
real. **El resto de caminos están escritos pero no se han ejercitado uno a uno**:
editar, fotos, publicar/despublicar, escalado, importación, alta y recuperación.

Se dice en lugar de darlos por buenos. Todos usan la misma pareja
`ClienteDeApi` + `AppViewModel` que sí se ejercitó, y los endpoints llevan
probados desde sus propias features, así que el riesgo es de detalle de interfaz,
no de diseño.
