# 023 · Cambiar contraseña desde dentro — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `IEnviadorDeCorreo.EnviarConfirmacionDeCambioDeContrasenaAsync`.

## Aplicación

- [x] `CambiarContrasena` — verifica la actual, valida la nueva, cambia y avisa.
- [x] Registrar en la inyección de dependencias.

## Infraestructura

- [x] Texto del aviso en `MensajesDeCorreo`.
- [x] Implementación en el enviador de Brevo y en el de consola.

## API

- [x] `LimitesDePeticiones.CambioDeContrasena`, con su propio cubo configurable.
- [x] `PeticionDeCambioDeContrasena` en `Recetas.Contratos.Contrasenas`.
- [x] `PUT /yo/contrasena`, autenticado y con límite de peticiones.

## Web

- [x] `ClienteDeApi.CambiarContrasenaAsync`.
- [x] Sección en `MiCuenta.razor`, entre los datos de la cuenta y la zona peligrosa.

## Pruebas

- [x] Aplicación: contraseña actual correcta y nueva válida cambia y permite iniciar sesión con la nueva.
- [x] Aplicación: la anterior deja de servir.
- [x] Aplicación: contraseña actual incorrecta no cambia nada.
- [x] Aplicación: nueva contraseña corta responde inválida y no cambia nada.
- [x] Aplicación: si el correo falla, **el cambio se mantiene**.
- [x] Integración: `PUT /yo/contrasena` sin sesión responde `401`.
- [x] Integración: contraseña actual incorrecta responde `401` con el mismo mensaje que la baja.
- [x] Integración: nueva contraseña corta responde `400`.
- [x] Integración: tras cambiarla, `POST /sesiones` con la nueva funciona y con la anterior falla.
- [x] Integración: superar el límite responde `429`.
- [x] Integración: el correo de aviso llega a la cuenta.

Comprobado que muerden: quitando la verificación de la contraseña actual falla
`ConLaActualIncorrecta_NoCambiaNada`; quitando la llamada al envío del correo
fallan `AlCambiar_AvisaPorCorreoALaCuenta` (aplicación) y
`AlCambiar_LlegaElCorreoDeAviso` (integración).

## Cierre

- [x] Suite completa en verde (615 pruebas, 0 fallos).
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Actualizar la lista de endpoints en `CLAUDE.md`.
- [x] Quitar la entrada del backlog en `roadmap.md` y añadir la feature a "Hecho".
- [ ] Desplegar.
- [x] Mirarlo en pantalla: comprobado en un dispositivo real el 9 de agosto de 2026. No se ha enviado el formulario contra la cuenta de producción, para no tocar una contraseña en uso.

## Mantenimiento (checklist recurrente)

- [ ] Si algún día se añade "cerrar sesiones al cambiar la contraseña" (entrada ya en el backlog), esta feature es uno de los sitios que tendría que invocarlo, junto con la 008 y la baja.
