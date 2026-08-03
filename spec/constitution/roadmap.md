# Roadmap

_Orden y estado de las features. Cada entrada apunta a su carpeta en `features/`._

## Hecho ✅

1. **[001 · Esqueleto y persistencia](../features/001-esqueleto-y-persistencia/spec.md)** — solución .NET con las capas hexagonales, PostgreSQL conectado, migración inicial y `GET /salud` recorriendo el camino completo. La regla de dependencias queda vigilada por tests.
2. **[002 · Cuentas de usuario](../features/002-cuentas-de-usuario/spec.md)** — alta en dos pasos con verificación por correo, inicio de sesión con JWT, límites de frecuencia y esqueleto de la web Blazor con sus tres pantallas. Queda pendiente de puesta en producción configurar SPF/DKIM y los secretos en el servidor.

3. **[003 · Recetas privadas](../features/003-recetas-privadas/spec.md)** — crear, editar, ver y borrar recetas propias, con ingredientes como entidad de catálogo compartido. Toda receta nace privada y un usuario no alcanza las de otro.

4. **[004 · Fotos](../features/004-fotos/spec.md)** — subir, servir y borrar imágenes desde el disco del servidor, por endpoint autenticado. El binario nunca entra en PostgreSQL.

5. **[005 · Publicar y despublicar](../features/005-publicar-y-despublicar/spec.md)** — transición de visibilidad y lectura de recetas ajenas publicadas. Incluyó la limpieza de metadatos EXIF de las fotos, que era su requisito previo: publicar sin quitarlos habría expuesto la ubicación de los usuarios.

6. **[006 · Búsqueda multicriterio](../features/006-busqueda-multicriterio/spec.md)** — por nombre, ingredientes y tipo de plato, combinables, insensible a mayúsculas y acentos. Alcanza las recetas propias y las publicadas por otros, nunca las privadas ajenas.

## Siguiente 🔜

7. **007 · Web completa** — el resto del cliente Blazor sobre el esqueleto que dejó la 002: recetario propio, ficha de receta, edición, fotos, publicación y búsqueda.

_El orden no es caprichoso: **005 depende de que existan recetas (003)**, y **006 solo tiene sentido cuando hay algo público que buscar (005)**. Las reglas de visibilidad se prueban en cuanto nacen, no al final._

_La web se parte en dos a propósito: la **002** levanta el esqueleto porque el enlace de verificación necesita una página donde aterrizar, y la **007** construye el resto una vez la API está completa. Entre medias (003–006) la validación es por tests de integración y peticiones HTTP directas, sin invertir en interfaz para features que aún se están moviendo._

## Backlog / ideas 💡

- **App Android** — Kotlin + Compose contra la misma API, **con publicidad AdMob**. Aplazada deliberadamente (ver `tech-stack.md`). Arrastra trabajo que no es de programación: política de privacidad publicada, declaración de datos en Google Play y plataforma de consentimiento para la UE. Contarlo en el alcance cuando llegue.
- **Recuperar contraseña** — mismo patrón que el alta (token de un solo uso por correo), reaprovechando la infraestructura de envío que trae la 002. Barata una vez hecha la 002.
- **Etiquetas libres** — el eje que `TipoPlato` deliberadamente no cubre: "ensalada", "sin gluten", "rápido", "de la abuela". Complemento del enumerado, no sustituto. Es la salida natural si al usar la app se echa en falta filtrar por algo que el momento del menú no expresa.
- **Escalado de fotos** — generar miniaturas para no servir la imagen completa en los listados.
- **Escalar cantidades** — ajustar los ingredientes al número de comensales.
- **Importar receta desde URL** — roza el límite de "no es un catálogo editorial" de `mission.md`; valorar con cuidado.

> Cada feature nueva se crea como `features/NNN-nombre-feature/` con `spec.md`, `plan.md` y `tasks.md` antes de tocar código.
