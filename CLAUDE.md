# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado actual del repositorio

**Las siete features del plan están terminadas, más la 008 (recuperar contraseña), la 009 (fotos en los listados), la 010 (escalar cantidades) y la 011 (importar desde URL), salidas del backlog.** El producto se usa entero desde el navegador. Lo siguiente vuelve a salir del backlog de `roadmap.md`.

**Web:** portada, alta en dos pasos, login, recuperar contraseña en dos pasos, recetario con miniaturas, ficha, crear/editar, fotos, publicar y buscar. La sesión vive en `localStorage`; `ManejadorDeAutenticacion` pone la cabecera en cada petición y centraliza el `401`, y `RutaProtegida` redirige al login. Ninguna página construye peticiones a mano.

Endpoints: `GET /salud`, `POST /registro/solicitudes`, `POST /registro/completar`, `POST /contrasena/solicitudes`, `POST /contrasena/restablecer`, `POST /sesiones`, `GET /yo`, `GET|POST /recetas`, `GET /recetas/busqueda`, `GET /recetas/favoritas`, `GET|PUT|DELETE /recetas/{id}`, `POST|DELETE /recetas/{id}/publicacion`, `PUT|DELETE /recetas/{id}/favorito`, `POST /recetas/importaciones`, `POST /recetas/{id}/fotos`, `GET|DELETE /recetas/{id}/fotos/{fotoId}` y `GET /recetas/{id}/fotos/{fotoId}/miniatura` (todos protegidos salvo salud, alta, recuperación de contraseña e inicio de sesión).

**Búsqueda:** hay columnas `nombre_para_busqueda` en `recetas` e `ingredientes`, con el texto en minúsculas y sin acentos. Se rellenan con `TextoParaBusqueda.Normalizar`, y **el texto guardado y el de la consulta deben pasar por esa misma función**: si divergen, las búsquedas no encuentran nada y no falla nada. No se usa la extensión `unaccent` porque instalarla exige privilegios que el despliegue no tiene.

**Dos flujos de token por correo, en tablas separadas a propósito:** `solicitudes_de_registro` (alta, 24 h) y `solicitudes_de_contrasena` (restablecimiento, 1 h). Comparten forma pero no significado; unificarlas con un campo "tipo" haría que cada consulta tuviera que acordarse de filtrarlo, y olvidarlo una vez permitiría canjear un enlace de alta por un cambio de contraseña ajena.

**Restablecer la contraseña no cierra las sesiones abiertas.** Un JWT emitido antes del cambio sigue valiendo hasta siete días. Es consecuencia de haber elegido JWT sin estado en la 002, está documentado en `spec/features/008-recuperar-contrasena/spec.md` y anotado en el backlog. No es un descuido: no lo "arregles" sin decidir antes el cambio de arquitectura.

## Permisos: tres preguntas distintas

- `Receta.EsDe(usuarioId)` — **solo el autor**. Gobierna editar, borrar, publicar, y subir o borrar fotos.
- `Receta.PuedeVerla(usuarioId)` — autor **o** receta publicada. Solo para leer la receta y descargar sus fotos.
- **Ser el responsable del servicio** (015) — no vive en la entidad: es el correo de `Moderacion:CorreoDelResponsable`, comparado con el del JWT en la capa de API. **Solo abre despublicar recetas públicas ajenas**, nunca editar, borrar, ni ver privadas. Se pasa como parámetro explícito a `CambiarVisibilidadAsync` para que no se cuele en otras operaciones, y hay tests de que no lo hace.

Confundirlas permitiría a cualquiera modificar recetas públicas ajenas. Los nombres son distintos a propósito.

La **única excepción** es la retirada por moderación, y **no es un parámetro de `GestionDeRecetas`**: vive en `RetirarPorModeracion` (020). Comparte la ruta `DELETE /recetas/{id}/publicacion` con el autor que deja de compartir —para quien pulsa es la misma acción— pero la retirada **avisa por correo al autor** y despublicar lo propio no. El endpoint intenta primero la vía del autor y solo cae en la moderación si la receta no es suya y quien pide es el responsable; así el responsable despublicando lo suyo no se autoenvía un aviso.

**Favoritos (021) — el filtro está en la lectura, no en la escritura.** La marca (`favoritos`, clave compuesta usuario+receta) **no se borra cuando el autor despublica**: quien decide qué sale es `ListarFavoritasAsync`, que comprueba la visibilidad **de ahora dentro de la consulta**. Si ese filtro se cae, marcar una receta antes de que la retiren se convierte en acceso permanente a ella. Marcar exige `PuedeVerla` —lo propio también vale—; desmarcar no exige nada, o quedaría una fila que su dueño no puede quitar. **No hay recuento de favoritos en ninguna respuesta**, y el hueco es deliberado: un contador es la valoración que `mission.md` descarta, con otro nombre.

**Denunciar (015) es lo que permite publicar en Play.** Play obliga a que una aplicación donde los usuarios comparten contenido tenga forma de denunciarlo **y** de actuar. `POST /recetas/{id}/denuncias` guarda la denuncia **y luego** avisa por correo: si el envío falla, la denuncia sigue existiendo. Denunciar dos veces lo mismo responde igual pero no duplica ni vuelve a avisar. Retirar **despublica, no borra**: el autor conserva su receta.

**Importar desde URL (011) — el único sitio donde el servidor pide una dirección que escribe un usuario.** Eso es SSRF, y `DescargadorDePaginasSeguro` es lo que lo contiene: `ConnectCallback` que valida **la IP a la que se conecta** (validar al resolver el nombre deja abierto el DNS rebinding), redirecciones a mano con tope porque cada salto hay que volver a comprobarlo, tope de bytes y de tiempo, y solo HTML. `ComprobadorDeDireccionesPublicas` tiene la lista de rangos; **cualquier cambio ahí va con test**. El error es **el mismo** para "dirección interna", "no responde" y "no existe": si distinguiera, el endpoint sería un escáner de la red del servidor. No quitar ninguna de esas defensas sin entender cuál tapa qué.

`POST /recetas/importaciones` **no crea nada**: devuelve un borrador que rellena el formulario. Es lo que mantiene la feature dentro del "no es un catálogo editorial" de `mission.md`, junto con que sea de una en una.

**Escalar cantidades (010):** `Receta.Raciones` es opcional; sin ese dato no se puede escalar y la ficha no lo ofrece. `GET /recetas/{id}?raciones=N` devuelve las cantidades ajustadas y **no escribe nada**: escalar es leer. El cálculo va en `EscaladoDeCantidades`, en el dominio, porque lo que importa no es multiplicar sino **redondear a cantidades medibles** —contables a media unidad, gramos y mililitros a entero, el resto a cuartos, y nunca a cero—, y eso es regla de negocio: en Blazor quedaría fuera del alcance de Android. `AlGusto` y `Pizca` no escalan. Se escala **siempre desde lo guardado**, nunca desde lo ya escalado, para no acumular redondeos.

**Miniaturas (009):** cada foto tiene una versión reducida en el mismo directorio, con sufijo `-min`. No lleva fila propia ni identificador: su ruta se deriva de la foto, y `Receta.FotoDePortada` —la más antigua— también es derivada, no un campo. Las fotos subidas antes de la 009 no tienen miniatura hasta que alguien la pide: se genera y se guarda en esa primera petición. **Esa generación perezosa va después de comprobar `PuedeVerla`**, o sería una puerta trasera a las fotos privadas ajenas.

**ImageSharp está fijado en la 3.1**: la 4.0 exige clave de licencia en tiempo de compilación y el proyecto no compila sin ella. Actualizar rompe el build, no solo la licencia.

## Dos trampas de EF que ya han mordido

- **Nunca acceder a `.Valor` de un objeto valor convertido dentro de una consulta LINQ** (`ingrediente.Nombre.Valor`). Compila y revienta en ejecución con "The LINQ expression could not be translated", que llega al cliente como un `500`. Se compara el objeto completo y EF aplica el conversor a los parámetros.
- **Las entidades con clave `Guid` generada por el dominio necesitan `ValueGeneratedNever()`** si se cuelgan de un padre ya rastreado. Si no, EF ve la clave rellena, deduce que la fila existe y emite `UPDATE` en lugar de `INSERT`; el error es un `DbUpdateConcurrencyException` de "0 filas afectadas" que no menciona la causa. A las que se añaden con `Add()` no les pasa, porque EF marca el grafo entero como nuevo.

La `constitution/` es la fuente de verdad: leerla antes de proponer nada. `spec/features/NNN-nombre-feature/` **no es una feature real**: es el molde a copiar.

## Flujo de trabajo obligatorio (SDD)

El proyecto sigue desarrollo dirigido por especificación: **primero spec, luego plan, luego tareas, y solo entonces código**. Antes de escribir cualquier implementación:

1. Crear `spec/features/NNN-nombre-feature/` con el siguiente número libre (`001`, `002`, …), copiando la plantilla.
2. `spec.md` — qué hace, por qué, criterios de aceptación verificables con sí/no.
3. `plan.md` — enfoque técnico, pasos con archivos afectados, decisiones y riesgos. Debe respetar `constitution/tech-stack.md`.
4. `tasks.md` — checklist accionable; marcar `[x]` según se avanza.
5. Implementar y validar.
6. Mover la feature a "Hecho" en `spec/constitution/roadmap.md`.

**La constitución manda:** si una feature choca con `mission.md` o `tech-stack.md`, se replantea la feature, no la constitución.

Si el usuario pide implementar algo sin spec previa, crear primero la carpeta de feature con los tres archivos.

## Qué es el producto

Recetario en la nube, **de acceso solo con cuenta**. El alta es en **dos pasos con verificación de correo**: el usuario indica su correo, recibe un enlace y al abrirlo elige contraseña; solo entonces existe la cuenta. Los usuarios guardan recetas con fotos. Cada receta es **privada** (nace así, solo el autor) o **pública** (visible para cualquier usuario registrado). No hay acceso anónimo a nada. Búsqueda por nombre, ingredientes y tipo de plato.

No es una red social (sin comentarios, valoraciones ni seguidores), ni planificador de menús, ni catálogo curado. Detalle en `spec/constitution/mission.md`.

## Arquitectura

Todo en C#/.NET 10 sobre un servidor de red local, expuesto vía túnel Cloudflare, despliegue por SSH.

- **API REST** — arquitectura hexagonal, EF Core, PostgreSQL, JWT. Fotos en el disco del servidor, nunca en la BD. Correo transaccional por la API HTTP de **Brevo**, tras el puerto `IEnviadorDeCorreo` (en desarrollo y tests se sustituye por un adaptador que no envía nada real).
- **Web** — Blazor WebAssembly. Cliente estático que consume la API por HTTP; sin reglas de negocio. CSS propio con tokens en variables y estilo aislado por componente: sin framework CSS ni biblioteca de componentes.
- **Android** — Kotlin + Compose en `android/`, **proyecto Gradle aparte de la solución .NET**: `dotnet build` no lo compila y `gradlew` no compila lo otro. Desde la 012 existe el esqueleto —sesión, recetario, ficha, búsqueda—; escribir recetas sigue siendo cosa de la web. Llevará **publicidad AdMob**, única monetización del producto y solo en esa superficie: la web y la API no la conocen ni la mencionan en el contrato. **Todavía no la lleva**, y añadirla no es solo código (ver el backlog).

Dos reglas que atraviesan todo el diseño:

1. **Las dependencias apuntan hacia dentro.** `Api` → `Aplicacion` → `Dominio`; `Infraestructura` implementa los puertos del dominio. Si el dominio necesita un `using` de EF o ASP.NET, el diseño está mal.
2. **La API se diseña como si Android ya existiera.** Nada de endpoints a medida de Blazor: un solo contrato para todas las superficies.

Los DTOs viven en un proyecto compartido (`Recetas.Contratos`) entre API y web, para que el contrato no se duplique.

## Reglas que no se negocian

Están en `spec/constitution/tech-stack.md` (secciones "Convenciones" y "Límites duros"). Las de mayor impacto al escribir código:

- Autorización **siempre** en servidor. Blazor WASM corre en el navegador del usuario: su código es visible y manipulable.
- **Todo endpoint exige JWT salvo registro e inicio de sesión.** Sin sesión, `401`. Incluye las fotos: la carpeta de imágenes no se sirve como estáticos, sale por endpoint autenticado.
- Pedir una receta privada ajena devuelve `404`, no `403` — un `403` confirmaría que existe.
- Contraseñas solo con derivación con sal; nunca en claro ni con hash rápido.
- El token de verificación del alta es un **secreto**: generador criptográfico (no `Guid`/`Random`), guardado hasheado, de un solo uso y con caducidad. Fuera de los logs.
- El paso 1 del alta responde igual exista o no ya ese correo — si no, filtra qué direcciones tienen cuenta.
- Una receta pública muestra su contenido, no el correo de su autor.

## Comandos

La solución es `Recetas.slnx` (formato .NET 10), **no** `.sln`. La app Android tiene sus propios comandos, en `android/README.md`: usa el **JDK 21 de Android Studio** (no el del sistema) y llega a la API local con `adb reverse tcp:5199 tcp:5199`, no con `10.0.2.2`, porque esa entra por la red y la para el cortafuegos.

```
docker compose up -d                       # PostgreSQL local — puerto 5433 del host, no 5432
dotnet run --project src/Recetas.Api       # API en http://localhost:5199
dotnet run --project src/Recetas.Web       # web en http://localhost:5200 (necesita la API)
dotnet test                                # suite completa (integración necesita Docker)
dotnet test --filter "Categoria!=Integracion"             # sin Docker
dotnet test --filter "FullyQualifiedName~NombreDelTest"   # un test concreto
dotnet format                              # estilo
```

En desarrollo **no se envía correo real**: el enlace de alta se escribe en el log de la API con el prefijo `[CORREO SIMULADO]`. De ahí se saca el token para completar un alta en local.

El 5432 lo ocupa otro proyecto en esta máquina; por eso el compose publica el 5433. Los comandos `dotnet ef` necesitan `ASPNETCORE_ENVIRONMENT=Development` para ver la cadena de conexión local.

Lista completa, incluidas migraciones de EF, en la sección "Comandos" de `tech-stack.md`. Mantener ambos sitios sincronizados.

## Despliegue

`./deploy/publish.ps1` compila, empaqueta y activa en `agfserver-angel`. Detalle y pasos previos con `sudo` en `deploy/README.md`.

Cadena: Cloudflare → cloudflared → nginx :80 → Kestrel `127.0.0.1:54009`. Web en `https://recetas.angelgf.com.es`, API en `https://recetas-api.angelgf.com.es`.

Cosas que muerden si se ignoran:

- **`publish.ps1` debe guardarse como UTF-8 con BOM.** PowerShell 5.1 lee un `.ps1` sin BOM como ANSI, y un carácter multibyte puede convertirse en una comilla tipográfica que rompe el análisis del script. `.gitattributes` lo fija.
- **Los `.sh` deben ir con LF.** Con CRLF, Linux lee el shebang como `bash\r` y responde "no such file or directory", que no menciona los finales de línea.
- **El servicio corre como `webapps`** porque PostgreSQL usa autenticación `peer` sobre el socket Unix: con otro usuario la conexión se rechaza aunque la cadena sea correcta.
- **La URL de la API en la web se reescribe al empaquetar**, no por variable de entorno: Blazor WASM es estático y no tiene arranque en servidor.

## Tests de arquitectura

`tests/Recetas.Arquitectura.Tests` vigila la regla de dependencias con **dos enfoques que se complementan**, y hay que mantener los dos:

- `ReglaDeDependenciasTests` inspecciona el ensamblado compilado. Punto ciego: no ve un paquete declarado pero todavía sin usar, porque el compilador no emite esa referencia.
- `ReferenciasDeProyectoTests` lee los `.csproj`, así que detecta la infracción al declararla.

Al añadir un proyecto a la solución, extender ambos.

## Nomenclatura

Dominio y tipos **en español** (`Receta`, `TipoPlato`, `PublicarReceta`), coherente con la documentación. Las piezas del framework se dejan como vienen (`Program`, `DbContext`).

## Idioma

La documentación del proyecto está en español. Mantener specs, planes y tareas en español.
