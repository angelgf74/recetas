# 008 · Recuperar contraseña

**Estado:** hecho

## Qué hace

Permite volver a entrar a quien haya olvidado su contraseña, con el mismo
mecanismo que el alta: se pide desde la pantalla de inicio de sesión, llega un
enlace al correo, y al abrirlo se elige una contraseña nueva.

## Por qué

Es el hueco más grave que queda. Hace falta cuenta para ver cualquier receta, y
hoy quien olvide su contraseña **pierde su recetario para siempre**: no hay
ninguna vía de vuelta, ni desde la aplicación ni administrativa.

Además, la mecánica ya existe y está probada desde la 002 —token de un solo uso
enviado por correo—, así que el coste es bajo y el agujero, grande.

## Criterios de aceptación

### Pedir el restablecimiento

- [x] `POST /contrasena/solicitudes` con un correo registrado envía un enlace a ese buzón.
- [x] La respuesta es **idéntica** exista o no la cuenta: no se puede averiguar qué direcciones están registradas.
- [x] Un correo con formato inválido responde `400`.
- [x] Pedirlo de nuevo invalida el enlace anterior: solo el último funciona.
- [x] Superar el límite de peticiones responde `429` sin enviar más correos. Comparte cubo con el alta: agotarlo desde `/registro/solicitudes` bloquea también este endpoint.
- [x] Un correo con alta pendiente pero sin completar no recibe enlace de restablecimiento.

### Restablecer

- [x] `POST /contrasena/restablecer` con un token válido y una contraseña aceptable la cambia y responde `200`.
- [x] Después, se puede iniciar sesión con la contraseña nueva.
- [x] La contraseña anterior deja de servir.
- [x] Ese mismo token deja de funcionar: un segundo intento responde `400`.
- [x] Un token caducado responde `400` y no cambia nada. **Probado en la capa de aplicación**, con reloj controlado; a través de HTTP no se puede sin esperar una hora.
- [x] Un token inexistente o manipulado responde `400`, con el mismo mensaje que uno caducado.
- [x] Una contraseña más corta que el mínimo responde `400` y no cambia nada.
- [x] Equivocarse al escribir la contraseña nueva **no** consume el enlace.

### Caducidad

- [x] El enlace de restablecimiento caduca **antes** que el de alta: una hora, no veinticuatro. Hay un test que compara ambas vigencias, para que subir una no deje la otra atrás sin avisar.

### Web

- [x] La pantalla de inicio de sesión ofrece "¿Has olvidado la contraseña?". **No probado en el navegador.**
- [x] Hay una pantalla para pedirlo (`/contrasena`) y otra para elegir la contraseña nueva (`/contrasena/nueva`). **No probadas en el navegador.**
- [x] El enlace del correo aterriza directamente en la pantalla de elegir contraseña, con el token cargado. Un test comprueba la dirección que se envía; que la página la lea bien **no está probado en el navegador**.
- [x] Tras restablecerla, se llega al inicio de sesión con un aviso claro. **No probado en el navegador.**

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que comprueba que las respuestas del paso 1 son idénticas exista o no la cuenta.
- [x] Existe un test que comprueba que la contraseña antigua deja de valer, y que antes sí valía.
- [x] El token no se guarda en claro: la columna almacena su hash.

## Fuera de alcance

- **Cambiar la contraseña estando dentro** (sabiendo la actual). Es otra pantalla y otro caso de uso.
- **Cambiar el correo** de una cuenta.
- **Preguntas de seguridad** o cualquier otra vía de recuperación que no sea el correo.
- **Verificación en dos pasos.**

## ⚠ Limitación conocida: las sesiones abiertas sobreviven

Restablecer la contraseña **no cierra las sesiones ya iniciadas**. Un JWT emitido
antes del cambio sigue siendo válido hasta que caduque, hasta siete días después.

Importa en el escenario que motiva restablecer: si alguien te robó la contraseña
y entró, cambiarla le corta el acceso futuro pero **no le echa de la sesión que ya
tiene abierta**.

Cerrarlas exigiría dejar de confiar solo en la firma del token y comprobar algo
en la base de datos en cada petición —una marca de versión de credenciales, o una
lista de revocación—, que es precisamente lo que la feature 002 evitó al elegir
JWT sin estado. No se resuelve aquí porque cambia una decisión de arquitectura, no
porque se haya pasado por alto. Queda anotado para valorarlo junto con los tokens
de refresco, que están en el mismo backlog.
