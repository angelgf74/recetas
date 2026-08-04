# 008 · Recuperar contraseña — Plan

## Enfoque

Es la misma mecánica que el alta de la 002: un secreto de un solo uso enviado al
buzón, guardado hasheado, con caducidad. Lo que cambia es lo que hay al otro
lado —un `Usuario` que ya existe en lugar de uno por crear— y el margen de
tiempo, que aquí debe ser mucho más corto.

Se reutiliza todo lo que ya está probado: el generador de tokens, el hasheador de
contraseñas, el puerto de correo y el patrón de respuesta uniforme. No se
generaliza `SolicitudDeRegistro` para que sirva a los dos casos: comparten forma
pero no significado, y un enumerado de "tipo de solicitud" haría que cada consulta
tuviera que acordarse de filtrarlo. Un fallo ahí permitiría usar un enlace de alta
para cambiar la contraseña de otro.

## Implementación

### Dominio

1. `Usuarios/SolicitudDeContrasena.cs` — `Id`, `UsuarioId`, `HashDelToken`, fechas de creación, caducidad y consumo. Igual que `SolicitudDeRegistro` pero apunta a un usuario existente.
2. `Usuario.CambiarContrasena(hash, ahora)`.
3. Puerto `IRepositorioDeSolicitudesDeContrasena`.
4. `IEnviadorDeCorreo` gana `EnviarEnlaceDeContrasenaAsync`.

### Aplicación

5. `Contrasenas/SolicitarRestablecerContrasena.cs` — busca el usuario; si existe, invalida las solicitudes vivas, crea una y envía el enlace. **Devuelve siempre lo mismo.**
6. `Contrasenas/RestablecerContrasena.cs` — valida la contraseña nueva antes de tocar el token, busca la solicitud por el hash, la consume y cambia la contraseña.

### Infraestructura

7. Configuración EF y repositorio.
8. Textos del correo, en `MensajesDeCorreo` junto a los demás.
9. Migración `RecuperarContrasena`.

### API

10. `POST /contrasena/solicitudes` y `POST /contrasena/restablecer`, ambos sin autenticación y con límite de peticiones.

### Web

11. `/contrasena` para pedirlo y `/contrasena/nueva` para elegirla.
12. Enlace "¿Has olvidado la contraseña?" en el inicio de sesión.

## Decisiones

- **Una hora de caducidad, no veinticuatro.** El enlace de alta solo permite crear una cuenta que aún no existe; este permite **tomar el control de una que ya existe, con sus recetas dentro**. A mayor daño posible, menor ventana.
- **No se reutiliza `SolicitudDeRegistro`.** Comparten estructura pero no significado. Unificarlas con un campo "tipo" obligaría a que cada consulta recordara filtrarlo, y olvidarlo una vez permitiría canjear un enlace de alta por un cambio de contraseña ajena. Dos tablas separadas hacen ese error imposible.
- **La respuesta no cambia si el correo no existe**, igual que en el alta. Si difiriera, este endpoint sería un comprobador de qué direcciones tienen cuenta.
- **A un correo sin cuenta no se le envía nada.** En el alta sí se avisa al dueño del buzón, porque allí el mensaje es útil ("alguien intentó registrarse con tu dirección"). Aquí un correo a alguien que no tiene cuenta solo sería ruido, y confirmaría al atacante que su intento llegó a alguna parte.
- **La contraseña nueva se valida antes de tocar el token.** Al revés, un error de tecleo quemaría el enlace y obligaría a pedir otro. Es la misma decisión que ya se tomó en la 002, con su test.
- **El cambio de contraseña y el consumo del token se guardan juntos.** Si se consumiera el token y fallara el cambio, el usuario se quedaría sin enlace y con la contraseña vieja.

## Riesgos

- **Enumeración de cuentas.** Mitigación: respuesta uniforme y un test que compara ambas respuestas byte a byte.
- **El token viaja en una URL.** Igual que en el alta: por eso caduca en una hora, es de un solo uso, y no se registra en los logs.
- **Uso del endpoint para bombardear un buzón ajeno.** Mitigación: límite de peticiones, como en el alta.
- **Las sesiones abiertas sobreviven al cambio.** Ver el aviso de `spec.md`: es una consecuencia de usar JWT sin estado, no un descuido. Queda documentado, no resuelto.
