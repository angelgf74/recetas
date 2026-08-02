# 002 · Cuentas de usuario

**Estado:** implementado ✅

## Qué hace

Permite crear una cuenta e iniciar sesión. El alta ocurre en dos pasos con verificación del correo:

1. El usuario indica su correo electrónico. Recibe en ese buzón un enlace.
2. Al abrir el enlace elige su contraseña. Solo entonces existe la cuenta.

Con la cuenta creada puede iniciar sesión con correo y contraseña, y obtiene un token de acceso (JWT) que las features siguientes usarán para identificarle.

Incluye el esqueleto de la web Blazor con tres pantallas —solicitar alta, elegir contraseña e iniciar sesión— porque el enlace del correo necesita una página donde aterrizar.

## Por qué

Es la puerta del producto: sin cuentas no hay recetario propio, ni distinción entre privado y público, ni nada que proteger. Todas las features posteriores dependen de saber quién pide qué.

La verificación del correo no es un adorno: sostiene el principio de comunidad cerrada de `mission.md`. Si cualquiera pudiera registrar direcciones ajenas o inventadas, "público" dejaría de significar "visible para usuarios reales".

## Criterios de aceptación

### Alta — paso 1 (solicitud)

- [x] `POST /registro/solicitudes` con un correo válido crea una solicitud y envía un mensaje con el enlace.
- [x] La respuesta es **idéntica** tanto si el correo es nuevo como si ya tiene cuenta: no se puede averiguar qué direcciones están registradas.
- [x] Si el correo ya tiene cuenta, se le envía un aviso a ese buzón en lugar de un enlace de alta.
- [x] Un correo con formato inválido responde `400`.
- [x] Solicitar el alta de nuevo con el mismo correo invalida el token anterior: solo el último enlace funciona.
- [x] Superar el límite de solicitudes responde `429` sin enviar más correos.

### Alta — paso 2 (completar)

- [x] `POST /registro/completar` con un token válido y una contraseña aceptable crea el usuario y responde `200`.
- [x] Tras completarse, ese mismo token deja de funcionar: un segundo intento responde `400`.
- [x] Un token caducado responde `400` y no crea ninguna cuenta.
- [x] Un token inexistente o manipulado responde `400`, con el mismo mensaje que uno caducado.
- [x] Una contraseña más corta que el mínimo responde `400` y no crea la cuenta.
- [x] La contraseña se guarda derivada con sal: en la base de datos no aparece ni en claro ni con un hash simple.
- [x] El token no se guarda en claro: la columna almacena su hash.

### Inicio de sesión

- [x] `POST /sesiones` con credenciales correctas devuelve un JWT válido con la identidad del usuario.
- [x] Credenciales incorrectas responden `401` con el **mismo** mensaje tanto si el correo no existe como si la contraseña falla.
- [x] Un correo con solicitud pendiente pero alta sin completar no puede iniciar sesión.
- [x] El JWT lo acepta la API en un endpoint protegido de prueba, y una petición sin token a ese endpoint responde `401`.

### Web

- [x] La web Blazor arranca y ofrece las tres pantallas: solicitar alta, elegir contraseña e iniciar sesión.
- [x] El enlace del correo abre directamente la pantalla de elegir contraseña con el token ya cargado.
- [ ] Las tres pantallas se ven correctamente en un ancho de móvil. _**Sin verificar de verdad.** Se comprobó que las tres renderizan y funcionan en el navegador, pero al intentar estrechar la ventana la captura siguió saliendo a 1538 px, así que el ancho de móvil real no llegó a probarse. El layout está escrito móvil primero (columna única de 26 rem centrada), pero eso es una expectativa, no una comprobación._

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Ni en desarrollo ni en los tests se envía correo real.
- [x] La clave de API de Brevo y la de firma del JWT no están en ningún archivo versionado.
- [x] Las contraseñas no aparecen en los logs, y los tokens tampoco **en el camino de Brevo**. _Matiz importante: el enviador de consola **sí escribe el enlace con el token**, y es deliberado —sin eso no se podría completar un alta en local—. Por eso ese enviador nunca debe activarse en producción, donde el log sería una fuga. Brevo se activa por configuración explícita._

## Fuera de alcance

- **Recetas.** Llegan en la 003. Aquí el endpoint protegido es solo una prueba de que el JWT funciona.
- **Recuperar contraseña.** Reutilizará este mismo patrón, pero es una feature aparte del backlog.
- **Cambiar la contraseña o el correo** de una cuenta ya creada.
- **Cerrar sesión en el servidor / revocación de tokens.** El JWT vale hasta que caduca.
- **Tokens de refresco.** La sesión dura lo que dure el JWT; renovar sin volver a escribir la contraseña se valorará cuando moleste.
- **Borrar la cuenta.** Su invariante ("no quedan recetas huérfanas") no se puede cumplir ni probar hasta que existan recetas.
- **Diseño visual acabado.** La web es un esqueleto funcional con los tokens de estilo establecidos, no una interfaz pulida.
- **Configurar SPF/DKIM en el DNS.** Es trabajo en el panel de Cloudflare y en el de Brevo, no en el repositorio. Queda como requisito de puesta en producción.
