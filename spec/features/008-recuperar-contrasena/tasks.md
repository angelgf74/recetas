# 008 · Recuperar contraseña — Tareas

## Dominio

- [x] Entidad `SolicitudDeContrasena` con vigencia, consumo e invalidación.
- [x] `Usuario.CambiarContrasena`.
- [x] Puerto `IRepositorioDeSolicitudesDeContrasena`.
- [x] `EnviarEnlaceDeContrasenaAsync` en `IEnviadorDeCorreo`.

## Aplicación

- [x] `SolicitarRestablecerContrasena`, con respuesta uniforme.
- [x] `RestablecerContrasena`, validando la contraseña antes de consumir el token.

## Infraestructura

- [x] Configuración EF y repositorio.
- [x] Textos del correo.
- [x] Migración `RecuperarContrasena`.

## API

- [x] `POST /contrasena/solicitudes` y `POST /contrasena/restablecer`, con límite.

## Web

- [x] `/contrasena` y `/contrasena/nueva`.
- [x] Enlace desde el inicio de sesión.

## Validación

- [x] Tests de dominio de la solicitud y del cambio de contraseña.
- [x] Tests de aplicación de los dos casos de uso y sus errores.
- [x] Test de que las respuestas del paso 1 son idénticas exista o no la cuenta.
- [x] Test de que la contraseña antigua deja de valer.
- [x] Test de que el token no sirve dos veces.
- [x] Test de que una contraseña inválida no consume el enlace.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
