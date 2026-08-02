# 002 · Cuentas de usuario — Tareas

## Dominio

- [x] `CorreoElectronico` con validación y normalización a minúsculas.
- [x] `Contrasena` con la política de longitud.
- [x] Entidad `Usuario` con factoría.
- [x] Entidad `SolicitudDeRegistro` con vigencia y consumo.
- [x] Puertos: repositorios, correo, hasheador, generador de tokens, emisor de acceso y reloj.

## Aplicación

- [x] Caso de uso `SolicitarRegistro`.
- [x] Caso de uso `CompletarRegistro`.
- [x] Caso de uso `IniciarSesion`.

## Infraestructura

- [x] Configuraciones EF y repositorios.
- [x] `HasheadorPbkdf2`.
- [x] `GeneradorDeTokensAleatorios`.
- [x] `EmisorDeAccesoJwt`.
- [x] `EnviadorDeCorreoBrevo` y `EnviadorDeCorreoDeConsola`.
- [x] `RelojDelSistema`.
- [x] Migración `CuentasDeUsuario`.

## API

- [x] Endpoints de registro y de sesiones.
- [x] Autenticación JWT y endpoint protegido `GET /yo`.
- [x] Limitador de peticiones en el paso 1.
- [x] Validación de la configuración al arrancar (clave JWT presente y suficientemente larga).

## Web

- [x] Proyecto Blazor WebAssembly y referencia a `Recetas.Contratos`.
- [x] Tokens CSS y estilos base.
- [x] Pantalla de solicitar alta.
- [x] Pantalla de elegir contraseña (lee el token de la URL).
- [x] Pantalla de iniciar sesión.

## Validación

- [x] Tests de dominio: correo, contraseña, vigencia y consumo de la solicitud.
- [x] Tests de aplicación de los tres casos de uso, incluidos los caminos de error.
- [x] Test de que las respuestas del paso 1 son idénticas exista o no la cuenta.
- [x] Tests de integración del alta completa y del inicio de sesión.
- [x] Test de que `GET /yo` responde `401` sin token y `200` con él.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **`GuardarCambiosAsync` añadido al repositorio de solicitudes.** En la primera versión, consumir el token solo se persistía porque otro repositorio llamaba a `SaveChanges` sobre el mismo `DbContext`. Funcionaba por casualidad: reordenar dos líneas del caso de uso habría dejado un token gastado sin registrar y reutilizable para crear otra cuenta. Ahora es explícito.
- **Límites de peticiones configurables.** No previsto: con valores fijos, los tests de integración chocaban con el `429` al encadenar altas. Ahora se leen de configuración, lo que además permite un test dedicado que baja el límite a dos y comprueba el rechazo.
- **Se añadieron `Recetas.Infraestructura.Tests`** (criptografía real) y se recreó `Recetas.Dominio.Tests`, eliminado en la 001 por estar vacío.
- **La plantilla de Blazor trae Bootstrap**, que la constitución descarta. Se eliminó junto con las páginas de ejemplo antes de escribir nada.
- **El puerto de la web es el 5200**, no el 5162 de la plantilla, para que coincida con lo configurado en CORS y en el enlace del correo.
- **`[SupplyParameterFromQuery]` en lugar de `QueryHelpers`** para leer el token de la URL: es el mecanismo propio de Blazor y evita un paquete extra.

## Nota: un test que no probaba lo que decía

`TokenValido_CreaElUsuario` afirmaba `Assert.DoesNotContain(contrasena, usuario.HashDeContrasena)` y falló. La causa no era el código de producción sino el propio doble: `HasheadorFalso` devuelve `hash(<valor>)`, así que la contraseña aparecía dentro por construcción.

La aserción era engañosa en cualquier caso: con un doble trivial nunca podría demostrar nada sobre la solidez del hash. Se cambió por una que sí corresponde a esa capa (que la contraseña pasa por el hasheador), y la garantía criptográfica real se comprueba donde toca, en `HasheadorPbkdf2Tests`.

## Pendiente de puesta en producción

_No es código; sin esto la feature no funciona fuera de local._

- [ ] Autenticar el dominio remitente en Brevo con **SPF y DKIM** en el DNS de Cloudflare. Sin ello los correos van a spam y el alta se rompe en silencio.
- [ ] Definir `Jwt:ClaveDeFirma`, `Correo:ClaveDeApi` y `Correo:UsarBrevo=true` por variables de entorno en el servidor.
- [x] ~~Configurar `ForwardedHeaders` en la API~~ — hecho al preparar el despliegue: `ForwardLimit=1` y solo loopback como proxy de confianza, para que el limitador vea la IP real que nginx copia de `CF-Connecting-IP`.
