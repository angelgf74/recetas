# Tech stack y convenciones

_Cómo está construido el proyecto y las reglas que todo el código debe respetar. Es la referencia técnica que ningún plan de feature debería contradecir._

## Tecnologías
#APIREst
- **Lenguaje:** C#, .NET 10, arquitectura hexagonal, seguridad con JWT
- **Framework / runtime:** Entity Framework
- **Base de datos:** PostgreSQL
- **Almacenamiento de fotos:** carpeta en el disco del servidor. PostgreSQL guarda solo la referencia; el binario nunca entra en la BD.
- **Envío de correo:** **Brevo**, vía su API HTTP de correo transaccional (no el relé SMTP: mejores errores y no depende de puertos salientes). Detrás de un puerto de dominio (`IEnviadorDeCorreo`): el caso de uso de registro no sabe quién entrega el mensaje.
- **Tests:** xUnit. Dos niveles: **unitarios de dominio** (sin infraestructura, con dobles de los puertos) e **integración de endpoints** contra PostgreSQL real levantado con Testcontainers. Requiere Docker en la máquina de desarrollo.
- **Despliegue:** `agfserver-angel` en la red local, expuesto por túnel de Cloudflare. Kestrel en `127.0.0.1:54009` bajo systemd (`recetas-api`), con nginx delante. En producción PostgreSQL se conecta por **socket Unix con autenticación `peer`** como usuario `webapps`: sin contraseña, pero el servicio debe correr con ese usuario exacto.

_Acceso a las fotos detrás de un puerto de dominio (`IAlmacenDeFotos` o equivalente): el dominio no sabe que hay un sistema de archivos detrás. Si algún día se pasa a MinIO o S3, se cambia el adaptador y nada más._

_La copia de seguridad tiene **dos piezas**: el volcado de PostgreSQL y la carpeta de fotos. Un backup que solo cubra la base de datos deja las recetas sin imágenes._

# App web
- **Lenguaje:** C#
- **Framework / runtime:** Blazor WebAssembly (.NET 10). Cliente estático: no renderiza en servidor ni mantiene conexión SignalR.
- **Base de datos:** ninguna propia. Todo el estado vive en la API; la web no habla con PostgreSQL.
- **Tests:** bUnit para componentes con lógica propia. La mayor parte de la cobertura vive en la API: la web no contiene reglas de negocio que testear.
- **Despliegue:** Servidor en la red local, expuesto a internet a traves de tunel cloudflare. Despliegue por SSH

_La web consume la API REST por HTTP igual que la app Android: un único contrato de API para las dos superficies. Los modelos de petición/respuesta (DTOs) y sus validaciones viven en un proyecto .NET compartido entre API y web, para no duplicarlos._

# App Android — APLAZADA

_No se construye por ahora. La web Blazor WASM es responsive e instalable como PWA, y cubre el uso en móvil hasta que el producto esté validado. Android entra por el roadmap como una feature más, no antes._

- **Lenguaje:** Kotlin
- **Framework / runtime:** Jetpack Compose, app nativa contra la misma API REST (decisión provisional, se confirma al abordarla).
- **Base de datos:** por decidir (solo si hace falta caché offline).
- **Tests:** por decidir.
- **Monetización:** **publicidad con AdMob**, solo en Android. Ni la web ni la API llevan publicidad.
- **Despliegue:** distribución del APK, no por SSH.

### Publicidad (AdMob) — condiciones

_Aplican cuando se aborde la app; se anotan ahora para que la decisión no llegue con sorpresas._

- **Solo en Android.** El mismo contenido en la web se ve sin anuncios. Es una asimetría deliberada, no un olvido.
- **Nunca en la pantalla de receta durante la elaboración.** El principio "usable con las manos sucias" pesa más que la impresión publicitaria: nada de intersticiales al abrir una receta ni banners que tapen los pasos.
- **AdMob obliga a política de privacidad publicada.** Su SDK recoge identificadores de dispositivo y datos de uso: hay que publicar una política de privacidad y declarar la recogida de datos en la ficha de Google Play. Sin eso, la app no pasa revisión.
- **Consentimiento para usuarios del Espacio Económico Europeo.** Google exige una plataforma de consentimiento certificada para servir anuncios personalizados en la UE. Si el usuario lo rechaza, la app **debe seguir funcionando** con anuncios no personalizados.
- **Ninguna cuenta ni receta se comparte con la red publicitaria.** El SDK no recibe correos, contenido de recetas ni el JWT. La publicidad vive aislada del dominio.
- **La API no se entera de que hay publicidad.** Nada de endpoints, campos ni métricas de anuncios en el contrato: es un asunto exclusivo del cliente Android.
- **Clave de AdMob y configuración fuera del control de versiones**, como cualquier otra credencial.

**Consecuencia para el diseño de la API:** aunque Android no exista todavía, la API se diseña como si existiera. Nada de endpoints hechos a medida de Blazor ni de lógica de negocio que acabe en el cliente web: si mañana entra Android, debe bastarle el mismo contrato.

## Archivos / módulos clave

_Creada en la feature 001._

- `src/Recetas.Dominio/` — **el núcleo hexagonal**. Entidades, invariantes y los *puertos* (interfaces) que necesita: repositorios, almacén de fotos, hash de contraseñas. Sin dependencias de EF, de ASP.NET ni de ningún paquete de infraestructura.
- `src/Recetas.Aplicacion/` — casos de uso que orquestan el dominio (crear receta, publicar, buscar). Depende del dominio, nunca de los adaptadores.
- `src/Recetas.Infraestructura/` — *adaptadores de salida*: EF Core + PostgreSQL, almacén de fotos en disco, generación y validación de JWT.
- `src/Recetas.Api/` — *adaptador de entrada*: endpoints HTTP, autenticación, composición de dependencias, migraciones.
- `src/Recetas.Contratos/` — DTOs de petición/respuesta y sus validaciones. **Compartido entre API y web**, es el contrato público.
- `src/Recetas.Web/` — cliente Blazor WebAssembly. Consume `Recetas.Contratos`.
- `src/Recetas.Web/` — cliente Blazor WebAssembly. Consume `Recetas.Contratos`. Los tokens de estilo viven en `wwwroot/css/tokens.css`.
- `tests/` — un proyecto de test por capa; los de integración levantan PostgreSQL con Testcontainers. `Recetas.Arquitectura.Tests` vigila la regla de dependencias.
- `deploy/` — todo el despliegue, versionado: `publish.ps1` (local), `remote-activate.sh` (viaja dentro del paquete), `nginx/recetas`, `systemd/recetas-api.service` y el `README.md` con los pasos que exigen `sudo`.

**Regla de dependencias:** las flechas apuntan siempre hacia dentro. `Api` → `Aplicacion` → `Dominio`; `Infraestructura` → `Dominio` (implementa sus puertos). El dominio no apunta a nadie.

## Comandos

_Verificados con la feature 001. La solución es `Recetas.slnx` (formato de .NET 10), no `.sln`._

- `docker compose up -d` — levanta PostgreSQL en local. **Escucha en el puerto 5433 del host**, no en el 5432: ese lo ocupa otro proyecto en la máquina de desarrollo.
- `dotnet run --project src/Recetas.Api` — arranca la API en local (puerto 5199).
- `dotnet run --project src/Recetas.Web` — arranca la web (puerto 5200). Necesita la API en marcha.
- `dotnet test` — ejecuta toda la suite. Los tests de integración necesitan Docker en marcha.
- `dotnet test --filter "Categoria!=Integracion"` — solo los que no necesitan Docker.
- `dotnet test --filter "FullyQualifiedName~NombreDelTest"` — un test concreto o una clase.
- `dotnet format` — aplica el estilo de `.editorconfig`.
- `dotnet build -c Release` — compila para producción.
- `dotnet ef migrations add <Nombre> --project src/Recetas.Infraestructura --startup-project src/Recetas.Api` — crea una migración.
- `dotnet ef database update --project src/Recetas.Infraestructura --startup-project src/Recetas.Api` — aplica las migraciones pendientes.
- `./deploy/publish.ps1` — compila, empaqueta y despliega en el servidor. `-SoloEmpaquetar` para revisar el paquete sin subirlo. Ver `deploy/README.md`.

_Los comandos de `dotnet ef` necesitan `ASPNETCORE_ENVIRONMENT=Development` para encontrar la cadena de conexión local._

## Modelo de datos / dominio

- `Usuario` — identidad de la cuenta. El correo es único y es el identificador de acceso. La contraseña **nunca** se guarda en claro: solo su hash. **Un `Usuario` solo existe cuando el alta está completa**: correo verificado y contraseña puesta. No hay usuarios a medio crear ni hash de contraseña nulo.
- `SolicitudDeRegistro` — el estado intermedio del alta en dos pasos: correo indicado, hash del token de verificación, fecha de creación y de caducidad, y si ya fue consumida. **No es un `Usuario`**: mientras vive aquí, ese correo no puede iniciar sesión ni aparece en ninguna parte del producto. Al completarse se crea el `Usuario` y la solicitud se marca consumida.
- `Receta` — pertenece siempre a un `Usuario` (su autor). Campos centrales: nombre, ingredientes, elaboración, `TipoPlato` y `Visibilidad`.
- `Receta.Visibilidad` — `Privada` | `Publica`. **Nace `Privada`**; pasar a `Publica` es una acción explícita del autor y es reversible (despublicar). Determina quién puede leerla: `Privada` solo el autor; `Publica` cualquier **usuario registrado**. No hay lectura anónima en ningún caso.
- `Receta.TipoPlato` — **lista cerrada** (enumerado), no etiquetas libres. Valor único y obligatorio. Valores: `Entrante`, `PrimerPlato`, `PlatoPrincipal`, `Guarnicion`, `Postre`, `Bebida`, `Salsa`.

  **Criterio de la lista: el momento del menú, no la naturaleza del plato.** Por eso no hay `Ensalada`, `SopaOCrema` ni `Reposteria`: se solapaban con los momentos (una crema es un `PrimerPlato`, una tarta un `Postre`) y obligaban al usuario a elegir entre dos casillas igual de válidas, con el resultado de que la misma receta acaba clasificada distinto según quién la suba. Un solo eje, cero ambigüedad.

  Añadir un valor implica cambio de código y migración: no es configurable en tiempo de ejecución. Persistir como texto (nombre del valor), no como número: así una reordenación del enumerado no reescribe el significado de los datos ya guardados.
- `Ingrediente` — entidad propia, no texto libre dentro de la receta: la búsqueda por ingredientes lo exige. Una receta referencia varios ingredientes, cada uno con su cantidad y unidad.
- `Foto` — una receta puede tener fotos. **El binario no vive en PostgreSQL**: la base de datos guarda solo la referencia (ruta/identificador) y los metadatos; el archivo va al almacenamiento de ficheros.

### Alta de usuario (dos pasos)

1. El usuario indica **solo su correo**. La API crea una `SolicitudDeRegistro` y envía a ese buzón un enlace con el token.
2. El enlace abre una página donde el usuario **elige su contraseña**. Si el token es válido y no ha caducado, se crea el `Usuario` y la solicitud queda consumida.

Reglas que debe cumplir la implementación:

- **El token es un secreto**, no un identificador. Se genera con un generador criptográficamente seguro (nunca `Guid.NewGuid()` ni `Random`), y en la base de datos se guarda **hasheado**: quien lea la tabla no debe poder completar altas ajenas.
- **Un solo uso y con caducidad.** Consumido o vencido, deja de valer. Pedir el alta otra vez invalida el token anterior.
- **La contraseña viaja solo en el paso 2**, en el cuerpo de una petición `POST`. Nunca en la URL, ni en el enlace del correo.
- **El token va en la URL del enlace**, que es inevitable: por eso caduca pronto y es de un solo uso.
- **No revelar si un correo ya está registrado.** El paso 1 responde siempre lo mismo ("si el correo es válido, recibirás un mensaje"), tanto si el alta procede como si no. Si el correo ya tiene cuenta, se envía un aviso a ese buzón en lugar de un enlace de alta, y la respuesta de la API no cambia.
- **Limitar la frecuencia de solicitudes** por correo y por origen: sin freno, el endpoint del paso 1 es un cañón de correo contra terceros.
- **Las solicitudes caducadas se purgan**, no se acumulan indefinidamente.

**Sobre el envío con Brevo:**

- La **clave de API es un secreto**: por configuración, fuera del control de versiones, y solo en el servidor. Blazor WASM jamás la ve — el correo lo envía la API, nunca el cliente.
- **Autenticar el dominio remitente** (SPF y DKIM) en el DNS antes de dar por buena la feature. Sin eso los mensajes caen en spam y el alta se rompe en silencio. El DNS ya está en Cloudflare, así que los registros se añaden ahí.
- **El fallo de envío no debe dejar el alta en un estado ambiguo.** Si Brevo falla, el usuario tiene que poder reintentar la solicitud y recibir un enlace nuevo.
- **Verificar el límite de envíos** del plan contratado. Agotar la cuota deja el registro inutilizable: conviene registrar (log) los fallos de envío para enterarse antes que por los usuarios.
- En **desarrollo y en los tests no se envía correo real**: el adaptador se sustituye por uno que escribe el mensaje en consola o en memoria. El puerto `IEnviadorDeCorreo` existe precisamente para esto.

### Invariantes

- Solo el autor puede editar, publicar, despublicar o borrar su receta.
- Una receta `Privada` no aparece en ninguna búsqueda ni respuesta dirigida a alguien que no sea su autor.
- Borrar un usuario borra o anonimiza sus recetas: no quedan recetas huérfanas.
- **Toda la API exige autenticación salvo los endpoints de alta (los dos pasos) y el inicio de sesión.** No existe endpoint de lectura anónima: sin JWT válido, `401`. Esto incluye servir las fotos — una imagen de receta no puede quedar accesible por URL directa sin sesión.
- **No hay cuenta sin correo verificado.** La única vía de creación de un `Usuario` es consumir una `SolicitudDeRegistro` válida.

## Convenciones

- **Idioma:** el código, los tipos y el dominio se escriben en **español** (`Receta`, `TipoPlato`, `PublicarReceta`), coherente con la documentación. Las palabras propias del framework se dejan como vienen (`Program`, `DbContext`, `IServiceCollection`).
- **Nombres C#:** convención estándar de .NET — `PascalCase` para tipos, métodos y propiedades; `camelCase` para variables locales y parámetros; interfaces con prefijo `I`.
- **Tests:** en `tests/`, un proyecto espejo por capa (`Recetas.Dominio.Tests`). Nombre del test descriptivo del comportamiento, no del método: `RecetaPrivada_NoApareceEnBusquedaDeOtroUsuario`.
- **Validación:** la de forma (campos obligatorios, longitudes, formato de correo) va en los DTOs de `Recetas.Contratos`, y por tanto se aplica igual en la API y en la web. La de **reglas de negocio** (quién puede publicar, qué transiciones de visibilidad valen) vive en el dominio y **nunca** solo en el cliente.
- **Errores:** el dominio lanza excepciones propias o devuelve resultados explícitos; el adaptador HTTP las traduce a códigos de estado en un único punto. Los endpoints no construyen mensajes de error a mano cada uno por su cuenta.
- **No filtrar existencia:** pedir una receta privada ajena responde igual que pedir una inexistente (`404`), no `403`. Un `403` confirmaría que esa receta existe.
- **Autorización siempre en servidor:** que la web oculte un botón no es una medida de seguridad. Cada endpoint comprueba autoría por su cuenta.
- **Migraciones:** todo cambio de esquema pasa por una migración de EF con nombre descriptivo. Nada de tocar la base a mano.

## Estilo visual

**CSS propio con tokens.** Sin framework CSS ni biblioteca de componentes: la app tiene pocas pantallas y un framework aportaría más peso y aspecto genérico que ayuda. En Blazor WASM el peso de descarga se paga en el arranque.

- **Tokens en variables CSS**, declaradas una vez en `:root`: color, tipografía, escala de espaciado y radios. Ningún componente escribe un color o un tamaño a pelo; si hace falta un valor nuevo, se añade como token.
- **Estilo aislado por componente** (`Componente.razor.css`), que Blazor delimita solo. Lo global se reduce a los tokens, la tipografía base y el *reset*.
- **Móvil primero.** El caso de uso central es consultar una receta con el móvil apoyado en la encimera: se diseña para esa pantalla y se amplía hacia arriba, nunca al revés.
- **Legible a distancia de brazo y con las manos ocupadas.** Cuerpo de texto grande en la ficha de receta (más de lo que pediría una app de escritorio), interlineado holgado, y ingredientes y pasos como listas separadas y escaneables. Áreas táctiles amplias.
- **Contraste alto**, cumpliendo WCAG AA como mínimo. La cocina tiene mala luz, reflejos y manos mojadas.
- **Las fotos son contenido, no decoración.** El layout debe verse igual de terminado en una receta sin ninguna foto: nada de huecos rotos ni marcos vacíos.
- **Sin animación gratuita.** Transiciones solo donde aclaren un cambio de estado, y respetando `prefers-reduced-motion`.

_Paleta y tipografías concretas se eligen en el `plan.md` de la 002, que es donde nacen las primeras pantallas, y quedan registradas aquí como tokens una vez fijadas._

## Límites duros

- **Nunca guardar contraseñas en claro ni con hash rápido.** Solo algoritmo de derivación con sal, pensado para contraseñas.
- **Nunca guardar el token de verificación en claro, ni generarlo con `Guid` o `Random`.** Es un secreto: generador criptográfico y hash en base de datos.
- **Nunca escribir tokens ni contraseñas en los logs.** Las URLs de verificación llevan el token dentro: cuidado con registrar la petición entera.
- **Nunca subir secretos al repositorio.** Cadenas de conexión, clave de firma JWT y clave de API de Brevo van por configuración fuera del control de versiones.
- **Nunca confiar en el cliente.** Blazor WASM se ejecuta en el navegador del usuario: su código es visible y manipulable. Ninguna comprobación de permisos vive solo ahí.
- **Nunca meter binarios de foto en PostgreSQL.**
- **El dominio no importa infraestructura.** Si `Recetas.Dominio` acaba necesitando un `using` de EF Core o de ASP.NET, el diseño está mal: se define un puerto.
- **No añadir dependencias sin justificarlo** en el `plan.md` de la feature que las necesita.
- **No exponer datos de otros usuarios.** Una receta pública muestra su contenido; no el correo de su autor.
- **No servir la carpeta de fotos como archivos estáticos.** Sería una puerta trasera de lectura anónima que se salta el `401`: las imágenes salen por un endpoint autenticado que comprueba la visibilidad de su receta.
