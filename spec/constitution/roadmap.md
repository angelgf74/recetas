# Roadmap

_Orden y estado de las features. Cada entrada apunta a su carpeta en `features/`._

## Hecho ✅

1. **[001 · Esqueleto y persistencia](../features/001-esqueleto-y-persistencia/spec.md)** — solución .NET con las capas hexagonales, PostgreSQL conectado, migración inicial y `GET /salud` recorriendo el camino completo. La regla de dependencias queda vigilada por tests.
2. **[002 · Cuentas de usuario](../features/002-cuentas-de-usuario/spec.md)** — alta en dos pasos con verificación por correo, inicio de sesión con JWT, límites de frecuencia y esqueleto de la web Blazor con sus tres pantallas. Queda pendiente de puesta en producción configurar SPF/DKIM y los secretos en el servidor.

3. **[003 · Recetas privadas](../features/003-recetas-privadas/spec.md)** — crear, editar, ver y borrar recetas propias, con ingredientes como entidad de catálogo compartido. Toda receta nace privada y un usuario no alcanza las de otro.

4. **[004 · Fotos](../features/004-fotos/spec.md)** — subir, servir y borrar imágenes desde el disco del servidor, por endpoint autenticado. El binario nunca entra en PostgreSQL.

5. **[005 · Publicar y despublicar](../features/005-publicar-y-despublicar/spec.md)** — transición de visibilidad y lectura de recetas ajenas publicadas. Incluyó la limpieza de metadatos EXIF de las fotos, que era su requisito previo: publicar sin quitarlos habría expuesto la ubicación de los usuarios.

6. **[006 · Búsqueda multicriterio](../features/006-busqueda-multicriterio/spec.md)** — por nombre, ingredientes y tipo de plato, combinables, insensible a mayúsculas y acentos. Alcanza las recetas propias y las publicadas por otros, nunca las privadas ajenas.

7. **[007 · Web completa](../features/007-web-completa/spec.md)** — el resto del cliente Blazor sobre el esqueleto que dejó la 002: recetario propio, ficha, edición, fotos, publicación y búsqueda, con sesión persistente y rutas protegidas.

8. **[008 · Recuperar contraseña](../features/008-recuperar-contrasena/spec.md)** — enlace de un solo uso por correo para volver a entrar, con la mecánica de la 002 pero caducando en una hora en vez de veinticuatro. Primera feature salida del backlog. Deja anotado que restablecer **no cierra las sesiones ya abiertas**: es consecuencia de los JWT sin estado de la 002.

9. **[009 · Fotos en los listados](../features/009-fotos-en-los-listados/spec.md)** — el recetario y la búsqueda dejan de ser texto puro. Trae miniaturas generadas con ImageSharp, que es lo que hace viable pintar una foto por tarjeta: el archivo original llega a 8 MB y viaja en base64. Las fotos anteriores estrenan miniatura la primera vez que se piden, sin script de relleno.

10. **[010 · Escalar cantidades](../features/010-escalar-cantidades/spec.md)** — una receta puede decir para cuántas raciones es, y la ficha ajusta las cantidades a otro número de comensales. El valor no está en multiplicar sino en **redondear a cantidades medibles**, que es regla de negocio: por eso vive en el dominio y el ajuste se pide al servidor en lugar de calcularse en el navegador.

11. **[011 · Importar receta desde URL](../features/011-importar-desde-url/spec.md)** — pegar un enlace rellena el formulario leyendo el `schema.org/Recipe` de la página. **De una en una y sin guardar nada**: el usuario revisa y guarda, que es lo que la mantiene dentro del "no es un catálogo editorial" de `mission.md`. Estrena peticiones salientes desde el servidor a destinos que elige un usuario, con todo lo que eso obliga a defender (SSRF).

12. **[012 · Android: esqueleto usable](../features/012-android-esqueleto/spec.md)** — aplicación nativa en Kotlin + Compose contra la misma API: sesión, recetario, ficha y búsqueda. Se parte igual que se partió la web (002 esqueleto, 007 resto) y por lo mismo. **Sin AdMob**, que arrastra trabajo que no es código.

## Siguiente 🔜

_Las siete features del plan inicial están hechas. Lo siguiente sale del backlog._

_El orden no es caprichoso: **005 depende de que existan recetas (003)**, y **006 solo tiene sentido cuando hay algo público que buscar (005)**. Las reglas de visibilidad se prueban en cuanto nacen, no al final._

_La web se parte en dos a propósito: la **002** levanta el esqueleto porque el enlace de verificación necesita una página donde aterrizar, y la **007** construye el resto una vez la API está completa. Entre medias (003–006) la validación es por tests de integración y peticiones HTTP directas, sin invertir en interfaz para features que aún se están moviendo._

## Backlog / ideas 💡

- **Android: escribir** — crear y editar recetas, fotos, publicar, escalar cantidades e importar desde URL. Es a la 012 lo que la 007 fue a la 002.
- **Android: alta y recuperar contraseña** — necesitan enlaces profundos, porque el enlace del correo aterriza hoy en la web.
- **AdMob en Android** — la única monetización prevista. **No es solo código**: exige cuenta de publicador, identificador de aplicación real, política de privacidad publicada, declaración de datos en Google Play y plataforma de consentimiento para la UE. Contar todo eso en el alcance cuando se aborde.
- **Publicar en Google Play** — firma de la aplicación, ficha de la tienda y las declaraciones de arriba.
- **Cerrar sesiones al cambiar la contraseña** — hoy un JWT emitido antes del cambio sigue valiendo hasta siete días. Exige comprobar algo en la base de datos en cada petición (marca de versión de credenciales o lista de revocación), justo lo que la 002 evitó. Valorarlo junto con los tokens de refresco.
- **Cambiar la contraseña desde dentro** — sabiendo la actual, sin pasar por el correo. La 008 dejó fuera este caso.
- **Etiquetas libres** — el eje que `TipoPlato` deliberadamente no cubre: "ensalada", "sin gluten", "rápido", "de la abuela". Complemento del enumerado, no sustituto. Es la salida natural si al usar la app se echa en falta filtrar por algo que el momento del menú no expresa.
- **Elegir la foto de portada** — hoy la receta se representa con la primera que se subió. Que el autor designe otra es un campo nuevo y un control en la ficha (sale de la 009).
- **Visor de fotos** — abrir una foto a tamaño completo desde la ficha. Hace falta antes de poder pasar la ficha a miniaturas, porque un enlace normal no vale: el endpoint exige cabecera de autorización.
- **Convertir unidades al escalar** — que 1000 g pasen a 1 kg cuando se dobla la receta. Exige una tabla de equivalencias y decidir cuándo conviene cambiar de unidad (sale de la 010).
- **Importar también la foto de la receta** — descargar y republicar la imagen de un tercero tiene más aristas que el texto; quedó fuera de la 011.
- **Más formatos de marcado al importar** (microdatos, RDFa) y páginas que montan la receta con JavaScript. Hoy solo se lee JSON-LD.

> Cada feature nueva se crea como `features/NNN-nombre-feature/` con `spec.md`, `plan.md` y `tasks.md` antes de tocar código.
