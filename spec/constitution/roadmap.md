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

19. **[019 · Exportar mis datos](../features/019-exportar-mis-datos/spec.md)** — descargar en un `.zip` todas tus recetas y tus fotos, con un `datos.json` legible y un `LEEME.txt` que lo explica. Es el derecho de portabilidad del RGPD que la 016 dejó fuera, y la otra mitad de "el autor manda sobre sus datos": irse sin poder llevarse nada es una puerta cerrada con llave. El paquete se escribe sobre la respuesta según se genera, sin cargar las fotos en memoria.

20. **[020 · Aviso de retirada](../features/020-aviso-de-retirada/spec.md)** — cuando el responsable retira una receta de la parte pública, su autor recibe un correo: qué receta, que no se ha borrado nada y a quién escribir. Es lo que separa moderar de censurar, y la 015 lo dejó anotado a propósito. De paso saca la retirada de `GestionDeRecetas`: mientras era idéntica a que el autor dejara de compartir, un parámetro `esResponsable` bastaba; en cuanto una avisa por correo y la otra no, dejaron de ser la misma operación y comparten solo la ruta.

21. **[021 · Favoritos privados](../features/021-favoritos-privados/spec.md)** — marcar una receta para volver a encontrarla, y una pantalla con lo marcado. **Solo lo ve quien lo marca**: no hay recuento ni forma de que el autor se entere, y ese hueco en el contrato es la feature, no un detalle.

    Nació como "valorar recetas de 0 a 5 estrellas" y se cambió a propósito. `mission.md` descarta las valoraciones por su nombre, y el motivo está en la frase siguiente: *"público significa consultable, no conversable"*. Una nota numérica, aunque no lleve texto, hace competir a las recetas entre sí. Los favoritos resuelven el caso de uso real —volver a lo bueno que te encontraste— sin nada de eso. Con las estrellas se cayó también **"3 recetas con 4 o más estrellas no ven publicidad"**: atar la monetización a una puntuación pública la vuelve falsificable con un par de cuentas, porque el alta solo pide un correo.

    La marca **sobrevive a que la receta deje de ser visible, pero la lista no la enseña**: el filtro de visibilidad va en la consulta, o los favoritos serían una forma de seguir viendo lo que alguien dejó de compartir.

22. **[022 · Visor de fotos](../features/022-visor-de-fotos/spec.md)** — la ficha pasa a una tira de miniaturas y la foto se abre a tamaño completo sobre la página. La ganancia no es el visor: es que **la ficha deja de descargar todos los archivos enteros** —y en base64, un 33 % más— solo para enseñar la receta. No se podía hacer antes porque el endpoint de fotos exige cabecera de autorización y una imagen no se puede enlazar, así que sin visor la foto grande quedaba inalcanzable. Ni un endpoint nuevo: todo estaba desde la 009. Teclado y foco con Blazor, **sin JavaScript**, porque la política de seguridad de contenido no admite `unsafe-inline` y eso falla solo en producción.

23. **[023 · Cambiar contraseña desde dentro](../features/023-cambiar-contrasena-desde-dentro/spec.md)** — con la sesión iniciada, cambiarla sabiendo la actual, sin pasar por el correo. La 008 dejó este caso fuera a propósito. Sigue el mismo orden que `BorrarCuenta`: se verifica la contraseña actual antes que validar la nueva, al revés que en el restablecimiento por correo, donde no hay ningún token de un solo uso que proteger de una errata. Avisa por correo al terminar, mismo patrón que la baja y la retirada por moderación: quien no lo pidió sabe que su contraseña está comprometida justo cuando más lo necesita.

24. **[024 · Etiquetas libres](../features/024-etiquetas-libres/spec.md)** — palabras libres sobre una receta ("sin gluten", "rápido", "de la abuela"), complemento de `TipoDePlato` y no sustituto. Calco exacto del catálogo de ingredientes (003/006): una entidad de vínculo explícita (`EtiquetaDeReceta`) en lugar de un many-to-many implícito de EF, precisamente para no reabrir el terreno de la trampa de EF que documenta este archivo — clave `Guid` puesta por el dominio colgada de un padre ya rastreado. Buscar por etiqueta usa el mismo filtro **Y** que los ingredientes, y hereda su garantía de seguridad: el filtro de visibilidad va primero en la misma consulta, así que una etiqueta de una receta privada ajena nunca se puede usar para inferir nada.

25. **[025 · Elegir la foto de portada](../features/025-elegir-foto-de-portada/spec.md)** — el autor designa cuál de sus fotos representa a la receta, en vez de que sea siempre la más antigua. `Receta.FotoDePortada` pasa de puramente derivada a tener una preferencia opcional por delante, con caída automática a la derivada si la elegida se borra. La columna que guarda la elección lleva clave foránea a `fotos` con `ON DELETE SET NULL` — una referencia cruzada entre `recetas` y `fotos` a propósito, con red por partida doble: el dominio limpia la referencia en `QuitarFoto`, y la relación de EF hace *fixup* del lado cliente aunque nadie lo llame. Comprobado quitando cada red por separado: ninguna de las dos es la única que sostiene el comportamiento observable, y hizo falta un test que mirara el campo interno para pillar la que sí.

26. **[026 · Convertir unidades al escalar](../features/026-convertir-unidades-al-escalar/spec.md)** — 500 g que se doblan pasan a leerse "1 kg", no "1000 g". Método nuevo (`EscalarConUnidad`) en vez de un parámetro booleano en `Escalar`, mismo criterio que separó `RetirarPorModeracion` en la 020: dos comportamientos que dejan de significar lo mismo no comparten camino. Solo se activa al pedir raciones explícitamente — la ficha en reposo y el formulario de edición piden lo mismo sin ese parámetro, y convertir siempre habría hecho que editar una receta guardada en gramos mostrara kilogramos y guardarla sin tocar ese campo cambiara la unidad guardada por el simple hecho de haberla mirado.

27. **[027 · Importar también la foto](../features/027-importar-tambien-la-foto/spec.md)** — la imagen del `Recipe` de `schema.org` se descarga, se limpia de metadatos y viaja en el borrador; el cliente la sube tras crear la receta, porque `POST /recetas/importaciones` sigue sin crear nada. Ni un `HttpClient` nuevo ni un `ConnectCallback` nuevo: la descarga de la imagen reutiliza el mismo cliente endurecido de la 011, con el núcleo de descarga extraído a un método compartido.

    **Nota sobre cómo se comprobó de verdad**, porque es una lección que vale para más que esta feature: los primeros tests contra `127.0.0.1` y direcciones internas fijas pasaban igual con protección que sin ella, porque en la máquina de desarrollo no hay nada escuchando ahí — "bloqueado" y "no hay nadie" dan el mismo resultado observable. El test que sí distingue levanta un servidor de verdad en bucle local, listo para responder, y comprueba que el descargador nunca llega a hablar con él. Comprobado saboteando el código a propósito: con un cliente sin protección, ese test falla; los de direcciones fijas no se enteran.

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

- ~~**Integración continua.**~~ **Hecha.** `.github/workflows/verificar.yml`, en
  cada empujón y en cada solicitud de cambios: la solución .NET —con la suite
  entera, porque el runner trae Docker— y la aplicación Android en depuración.
  **No despliega nada**: desplegar sigue siendo deliberado con `publish.ps1`, que
  además exige el árbol limpio para que la release sea reproducible desde git.

- ~~**Comprobar que el límite de peticiones distingue a los usuarios.**~~
  **Comprobado el 9 de agosto de 2026 contra producción, y funciona.** Se agotó
  el límite de inicio de sesión desde una IP externa (10 permitidos, el 11 con
  `429`) y acto seguido una petición **directa a Kestrel desde el servidor**
  respondió `401`, no `429`.

  Eso lo demuestra: sin las cabeceras de reenvío, todo el tráfico externo llegaría
  a Kestrel como `127.0.0.1` —cloudflared y nginx están ambos en local—, o sea el
  mismo cubo que la petición local, y habría salido limitada.

- **El registro de nginx no guarda la IP real.** Anota `127.0.0.1` en todas las
  peticiones, porque usa el formato `combined` por defecto y `$remote_addr` es el
  túnel. El limitador sí ve la IP verdadera —va por `X-Forwarded-For`—, pero **si
  algún día hay abuso, el registro no dice de dónde viene**. Se arregla con un
  `log_format` que incluya `$http_cf_connecting_ip`. Afecta a todas las
  aplicaciones del servidor, no solo a esta.

### Producto y mantenimiento

- ~~**Publicar en Google Play**~~ — **hecho el 7 de agosto de 2026**, en producción y solo para España: [com.angelgf.recetas](https://play.google.com/store/apps/details?id=com.angelgf.recetas). El camino completo, con lo que exigió cada declaración, queda en `android/play/publicar.md`.

  **Los enlaces profundos, comprobados el 9 de agosto de 2026 con la aplicación instalada desde la tienda.** `pm get-app-links` pasó de `1024` —verificación fallida, con la copia instalada por cable— a `verified`, y abrir una dirección del dominio lanza la aplicación en lugar del navegador.

  Eso confirma de paso la decisión delicada de aquel día: en `assetlinks.json` va la huella del certificado con el que **Play refirma** (`4A:7E:85:…`), no la de subida. Con la de subida seguiría fallando, y sin ningún error que lo explicara: el enlace simplemente abriría el navegador.
- **Cerrar sesiones al cambiar la contraseña** — hoy un JWT emitido antes del cambio sigue valiendo hasta siete días. Exige comprobar algo en la base de datos en cada petición (marca de versión de credenciales o lista de revocación), justo lo que la 002 evitó. Valorarlo junto con los tokens de refresco.
- **Más formatos de marcado al importar** (microdatos, RDFa) y páginas que montan la receta con JavaScript. Hoy solo se lee JSON-LD.

- **Android sin conexión.** Es la mejora de producto con más recorrido: un
  recetario se usa **en la cocina**, que es donde peor llega el wifi, y hoy sin
  red no se ve ni una receta ya consultada. Guardar en el dispositivo lo que se
  ha abierto —texto y miniatura— cubre el caso real sin sincronización completa.
  Obliga a decidir qué se enseña cuando lo local está desactualizado.

- **La clave primaria se llama `"Id"` y el resto de columnas van en snake_case.**
  La configuración de EF renombró todo menos la clave, así que PostgreSQL la
  creó entrecomillada y es sensible a mayúsculas: cualquier consulta escrita a
  mano falla con `column "id" does not exist`, que no explica por qué. A EF le da
  igual —siempre entrecomilla—, pero muerde a quien entra por `psql`. Arreglarlo
  es una migración de renombrado, y **no se hace justo antes de publicar**.

- **Actualizar ImageSharp a la 4.0, cuando llegue la licencia comunitaria.** Solicitada el 8 de agosto de 2026; hay clave de prueba hasta el **6 de noviembre**. **No migrar antes de tener la definitiva**: la 4.0 no compila sin clave, así que hacerlo con la de prueba pone fecha de caducidad al build. Sin urgencia, además: la 3.1 sigue recibiendo parches de seguridad. Cuando toque, la clave va fuera del control de versiones —el repositorio es público— y hay que revisar los cambios de API.

- **Paginar la búsqueda.** Hoy hay un tope de 50 resultados y un aviso de que se
  recortó, que es la mitad barata del problema. Entra cuando ese tope moleste.
- **Exportar los favoritos con el resto de los datos (019)** — hoy el paquete no los lleva. Un favorito ajeno es una referencia a contenido de otra persona: exportar solo identificadores no sirve de nada, y exportar los nombres metería contenido ajeno en el paquete de uno. Sale de la 021.

- **Enviar sugerencias** — desde la aplicación a `POST https://gestorsugerencias-api.angelgf.com.es/api/sugerencias`. Tres decisiones antes de escribir código: **quién llama** (si el cliente, ese servicio necesita CORS; si la API, es una dependencia saliente y hay que decidir qué pasa cuando esté caída), **qué se envía** (si va el correo del usuario, es un tercero nuevo que entra en la tabla de la política de privacidad) y **cómo se autentica**, porque un buzón de sugerencias abierto es spam ajeno.

- **Exención de publicidad por recetas compartidas** — quien tenga diez recetas públicas deja de ver anuncios. **Se puede hacer sin tocar la API**: Android ya recibe la visibilidad de cada receta en `GET /recetas` y puede contarlas él, así que `tech-stack.md` sigue intacto.

  Dos objeciones antes de hacerla: premiar por **cantidad** empuja a publicar por publicar, que es justo el contenido que la 015 obliga a moderar; y si alguien despublica una receta y baja de diez, **le vuelven los anuncios**, que se lee como castigo y no hay forma de explicarlo bien en la interfaz.

- **Exención de publicidad manual (`SinPublicidad` en `usuarios`)** — marcar cuentas concretas que no ven anuncios. **Choca con `tech-stack.md`**, que dice que la API no se entera de que hay publicidad: no hay forma elegante de esquivarlo, porque es un campo sobre publicidad en el contrato, y ponerle un nombre neutro solo escondería la excepción. Hacerla es **cambiar la constitución**, no añadir una feature. Antes conviene concretar para quién es: si son unas pocas cuentas conocidas, puede que no haga falta el campo.
> Cada feature nueva se crea como `features/NNN-nombre-feature/` con `spec.md`, `plan.md` y `tasks.md` antes de tocar código.
