# 012 · Android: esqueleto usable — Tareas

## Proyecto

- [x] Envoltorio de Gradle, `settings.gradle.kts` y catálogo de versiones.
- [x] `build.gradle.kts` de la aplicación, con Compose y Ktor.
- [x] `.gitignore` de Android; `local.properties` fuera del repositorio.
- [x] `AndroidManifest.xml` con permiso de red y sin copia de seguridad automática.
- [x] `README.md` con el arranque, incluido `adb reverse`.

## Datos

- [x] `SesionLocal`, cifrando con el almacén de claves del sistema.
- [x] `ClienteDeApi`: cabecera en un solo sitio, `401` centralizado, errores traducidos.
- [x] Modelos espejo de `Recetas.Contratos`, tolerantes a campos nuevos.
- [x] Descarga de fotos y miniaturas con la cabecera de autorización.

## Interfaz

- [x] Pantalla de inicio de sesión.
- [x] Recetario con miniaturas.
- [x] Ficha de receta, que mantiene la pantalla encendida.
- [x] Búsqueda por nombre e ingredientes.
- [x] Navegación y cierre de sesión.

## Validación

- [x] Test de que la sesión se guarda y se borra.
- [x] Test de la traducción de errores HTTP.
- [x] Test de que un campo desconocido en la respuesta no rompe la aplicación.
- [x] `gradlew assembleDebug` produce un APK (13 MB).
- [x] `gradlew test` en verde (11 tests), sin avisos.
- [x] Probado en un emulador: sesión, recetario, ficha, búsqueda, persistencia y giro.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
