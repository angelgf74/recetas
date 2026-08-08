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

13. **[013 · Anuncios en Android](../features/013-anuncios-en-android/spec.md)** — banner de AdMob al pie del recetario y de la búsqueda, y **ninguno en la ficha**, que es la pantalla que se lee cocinando. Consentimiento del RGPD con UMP antes de pedir el primer anuncio, y opción de revocarlo desde Ajustes, que Play exige. La web no lleva publicidad ni la llevará.

14. **[014 · Android: paridad con la web](../features/014-android-paridad-con-la-web/spec.md)** — crear y editar recetas, fotos, publicar, escalar cantidades e importar desde URL. Es a la 012 lo que la 007 fue a la 002.

15. **[015 · Denunciar contenido](../features/015-denunciar-contenido/spec.md)** — denunciar una receta pública ajena, con aviso por correo al responsable y retirada por su parte. Sale de una exigencia concreta: Play no publica una aplicación donde los usuarios comparten contenido si no hay forma de denunciarlo **y** de actuar. Estrena una tercera pregunta de permisos —quién modera— que deliberadamente **no** vive en el dominio y solo abre despublicar.

16. **[016 · Darse de baja](../features/016-darse-de-baja/spec.md)** — borrar la cuenta desde la aplicación, con la contraseña como confirmación, más la página pública que Play exige. El borrado lo orquesta la aplicación y no la base de datos: `recetas.autor_id` no tiene clave foránea, así que borrar al usuario dejaría sus recetas vivas y sin dueño, y ninguna cascada habría tocado los archivos del disco.

17. **[017 · Salud del almacenamiento](../features/017-salud-del-almacenamiento/spec.md)** — `GET /salud` comprueba también el disco de las fotos, no solo la base de datos, y dice qué pieza falla. Es lo que hace útil una sonda externa: un monitor que mira el código HTTP no puede saber nada del disco por su cuenta. De paso destapó que el almacén de fotos no creaba su directorio al arrancar —los *singleton* se construyen perezosamente—, así que `/salud` habría dado un falso positivo tras cada despliegue.

18. **[018 · Tests de la web](../features/018-tests-de-la-web/spec.md)** — bUnit para los componentes donde una condición decide qué acciones se ofrecen, que es donde ha fallado dos veces. Y de rebote lo que más urgía: al referenciar `Recetas.Web`, **`dotnet test` por fin la compila**. Antes no lo hacía, comprobado rompiéndola a propósito: 540 pruebas en verde y salida `0`. Un test de arquitectura vigila que esa referencia no desaparezca.

## Siguiente 🔜

_Las siete features del plan inicial están hechas. Lo siguiente sale del backlog._

_El orden no es caprichoso: **005 depende de que existan recetas (003)**, y **006 solo tiene sentido cuando hay algo público que buscar (005)**. Las reglas de visibilidad se prueban en cuanto nacen, no al final._

_La web se parte en dos a propósito: la **002** levanta el esqueleto porque el enlace de verificación necesita una página donde aterrizar, y la **007** construye el resto una vez la API está completa. Entre medias (003–006) la validación es por tests de integración y peticiones HTTP directas, sin invertir en interfaz para features que aún se están moviendo._

## Backlog / ideas 💡

### Antes que ninguna feature nueva ⚠️ · las tres hechas

_Con la aplicación publicada hay datos de otras personas dentro. Estas tres no
añadían nada visible y eran las que evitan perderlo todo. Se cerraron el 7 y 8 de
agosto de 2026; se dejan aquí, tachadas, porque lo que hay que **mantener** vivo
—restaurar de vez en cuando, no ignorar los avisos— se olvida antes si el motivo
desaparece de la lista._

- ~~**Copias de seguridad.**~~ **Hecho y probado de extremo a extremo.** Las dos
  piezas —bases con `pgbackup.sh`, fotos con `deploy/backup-ficheros.sh`—,
  automáticas por cron y sincronizadas a OneDrive con `rclone`. El **8 de agosto
  de 2026** se recorrió el ciclo entero: copia local, subida, descarga desde
  OneDrive, extracción en limpio y `diff -r` contra el original. Idénticas.
  Conviene repetirlo de vez en cuando: lo que se estropea en silencio no es el
  script, es el testigo de acceso a OneDrive.

- ~~**El repositorio no tiene remoto.**~~ **Hecho.** Está en
  [github.com/angelgf74/recetas](https://github.com/angelgf74/recetas), público y
  con `README`. Se comprobó antes de subirlo que ni el almacén de firma, ni las
  credenciales de Brevo, ni ninguna contraseña habían entrado nunca en la
  historia. **Lo que sigue viviendo solo en un disco es el almacén de firma de
  Android** (`recetas-release.jks`), y con razón: quien lo tenga puede publicar
  actualizaciones en tu nombre. Guardarlo aparte es cosa tuya, no del
  repositorio.

- ~~**Vigilancia del servicio.**~~ **Hecho.** `/salud` comprueba base de datos y
  disco (017), y un monitor de UptimeRobot lo consulta cada cinco minutos desde
  fuera del servidor, avisando por correo si deja de responder `200`. Externo a
  propósito: un vigilante en la misma máquina se cae con ella.

### Calidad

_Las dos primeras salen de fallos que ya han ocurrido en este proyecto, no de
buenas prácticas en abstracto._

- ~~**Meter `Recetas.Web` en la cadena de verificación** y **tests de interfaz.**~~
  Las dos con la **018**: resultó que el arreglo natural de la primera —un
  proyecto de tests que referencia la web— era exactamente lo que hacía falta
  para la segunda.

- **Integración continua.** No hay ninguna: la suite se ejecuta cuando alguien se
  acuerda. Depende de tener un remoto (arriba).

- **Comprobar que el límite de peticiones distingue de verdad a los usuarios.**
  `LimitesDePeticiones` reparte por dirección de origen, y el propio código
  advierte de que tras el túnel de Cloudflare puede llegar siempre la misma IP.
  Se configuró `UseForwardedHeaders`, pero **no se ha comprobado en producción**:
  si no funciona, todos comparten cubo y basta uno para dejar fuera a los demás.

### Producto y mantenimiento

- **Publicar en Google Play** — lo único que queda es de consola, no de código: cuenta de desarrollador, ficha de la tienda, clasificación de contenido, seguridad de los datos, declarar que contiene anuncios y dar la URL de baja (`/borrar-cuenta.html`, que llegó con la 016). El paquete firmado ya se genera y está probado en un teléfono real. Pasos en `android/play/publicar.md`.
- **Avisar al autor de que su receta ha sido retirada** — hoy se entera al mirarla. Necesita decidir qué se le cuenta —sin exponer quién denunció— y si puede recurrir. Sale de la 015.
- **Cerrar sesiones al cambiar la contraseña** — hoy un JWT emitido antes del cambio sigue valiendo hasta siete días. Exige comprobar algo en la base de datos en cada petición (marca de versión de credenciales o lista de revocación), justo lo que la 002 evitó. Valorarlo junto con los tokens de refresco.
- **Cambiar la contraseña desde dentro** — sabiendo la actual, sin pasar por el correo. La 008 dejó fuera este caso.
- **Etiquetas libres** — el eje que `TipoPlato` deliberadamente no cubre: "ensalada", "sin gluten", "rápido", "de la abuela". Complemento del enumerado, no sustituto. Es la salida natural si al usar la app se echa en falta filtrar por algo que el momento del menú no expresa.
- **Elegir la foto de portada** — hoy la receta se representa con la primera que se subió. Que el autor designe otra es un campo nuevo y un control en la ficha (sale de la 009).
- **Visor de fotos** — abrir una foto a tamaño completo desde la ficha. Hace falta antes de poder pasar la ficha a miniaturas, porque un enlace normal no vale: el endpoint exige cabecera de autorización.
- **Convertir unidades al escalar** — que 1000 g pasen a 1 kg cuando se dobla la receta. Exige una tabla de equivalencias y decidir cuándo conviene cambiar de unidad (sale de la 010).
- **Importar también la foto de la receta** — descargar y republicar la imagen de un tercero tiene más aristas que el texto; quedó fuera de la 011.
- **Más formatos de marcado al importar** (microdatos, RDFa) y páginas que montan la receta con JavaScript. Hoy solo se lee JSON-LD.

- **Android sin conexión.** Es la mejora de producto con más recorrido: un
  recetario se usa **en la cocina**, que es donde peor llega el wifi, y hoy sin
  red no se ve ni una receta ya consultada. Guardar en el dispositivo lo que se
  ha abierto —texto y miniatura— cubre el caso real sin sincronización completa.
  Obliga a decidir qué se enseña cuando lo local está desactualizado.

- **Exportar tus datos.** Es el derecho de portabilidad del RGPD, que la 016
  dejó fuera a propósito: hoy se atiende escribiendo un correo y a mano. Un
  archivo con las recetas y las fotos, pedido desde la misma pantalla que la
  baja, lo resuelve. Conviene tenerlo **antes** de que alguien lo pida.

- **La clave primaria se llama `"Id"` y el resto de columnas van en snake_case.**
  La configuración de EF renombró todo menos la clave, así que PostgreSQL la
  creó entrecomillada y es sensible a mayúsculas: cualquier consulta escrita a
  mano falla con `column "id" does not exist`, que no explica por qué. A EF le da
  igual —siempre entrecomilla—, pero muerde a quien entra por `psql`. Arreglarlo
  es una migración de renombrado, y **no se hace justo antes de publicar**.

- **Actualizar ImageSharp a la 4.0, cuando llegue la licencia comunitaria.** Solicitada el 8 de agosto de 2026; hay clave de prueba hasta el **6 de noviembre**. **No migrar antes de tener la definitiva**: la 4.0 no compila sin clave, así que hacerlo con la de prueba pone fecha de caducidad al build. Sin urgencia, además: la 3.1 sigue recibiendo parches de seguridad. Cuando toque, la clave va fuera del control de versiones —el repositorio es público— y hay que revisar los cambios de API.

- **Paginar la búsqueda.** Hoy hay un tope de 50 resultados y un aviso de que se
  recortó, que es la mitad barata del problema. Entra cuando ese tope moleste.
- **Favoritos privados** — marcar una receta ajena publicada para volver a encontrarla. **Privado: solo lo ve quien lo marca**, no cuenta nada al autor ni se agrega en ninguna puntuación.

  Nació como "valorar recetas de 0 a 5 estrellas" y se cambió a esto a propósito. `mission.md` descarta las valoraciones por su nombre, y el motivo está en la frase siguiente: *"público significa consultable, no conversable"*. Una nota numérica, aunque no lleve texto, convierte el recetario en algo comparable y hace competir a las recetas entre sí. Los favoritos resuelven el caso de uso real —volver a lo bueno que te encontraste— sin nada de eso.

  Con ellas se cayó también **"3 recetas con 4 o más estrellas no ven publicidad"**: atar la monetización a una puntuación pública la vuelve falsificable con un par de cuentas, porque el alta solo pide un correo.

- **Enviar sugerencias** — desde la aplicación a `POST https://gestorsugerencias-api.angelgf.com.es/api/sugerencias`. Tres decisiones antes de escribir código: **quién llama** (si el cliente, ese servicio necesita CORS; si la API, es una dependencia saliente y hay que decidir qué pasa cuando esté caída), **qué se envía** (si va el correo del usuario, es un tercero nuevo que entra en la tabla de la política de privacidad) y **cómo se autentica**, porque un buzón de sugerencias abierto es spam ajeno.

- **Exención de publicidad por recetas compartidas** — quien tenga diez recetas públicas deja de ver anuncios. **Se puede hacer sin tocar la API**: Android ya recibe la visibilidad de cada receta en `GET /recetas` y puede contarlas él, así que `tech-stack.md` sigue intacto.

  Dos objeciones antes de hacerla: premiar por **cantidad** empuja a publicar por publicar, que es justo el contenido que la 015 obliga a moderar; y si alguien despublica una receta y baja de diez, **le vuelven los anuncios**, que se lee como castigo y no hay forma de explicarlo bien en la interfaz.

- **Exención de publicidad manual (`SinPublicidad` en `usuarios`)** — marcar cuentas concretas que no ven anuncios. **Choca con `tech-stack.md`**, que dice que la API no se entera de que hay publicidad: no hay forma elegante de esquivarlo, porque es un campo sobre publicidad en el contrato, y ponerle un nombre neutro solo escondería la excepción. Hacerla es **cambiar la constitución**, no añadir una feature. Antes conviene concretar para quién es: si son unas pocas cuentas conocidas, puede que no haga falta el campo.
> Cada feature nueva se crea como `features/NNN-nombre-feature/` con `spec.md`, `plan.md` y `tasks.md` antes de tocar código.
