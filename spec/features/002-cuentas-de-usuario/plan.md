# 002 · Cuentas de usuario — Plan

## Enfoque

Tres casos de uso (`SolicitarRegistro`, `CompletarRegistro`, `IniciarSesion`) apoyados en puertos del dominio, y adaptadores concretos en infraestructura. Todo lo delicado —hashear contraseñas, generar tokens, medir el tiempo, enviar correo, firmar JWT— entra al dominio como puerto, de modo que los casos de uso se prueban sin criptografía real, sin reloj real y sin red.

El reloj como puerto (`IReloj`) merece mención aparte: la caducidad de los tokens es una regla de negocio, y sin poder controlar el tiempo en los tests habría que dormir el hilo para probarla.

## Implementación

### Dominio (`Recetas.Dominio`)

1. `Usuarios/Usuario.cs` — entidad con `Id`, `Correo`, `HashDeContrasena` y `FechaDeAlta`. Constructor privado y factoría `Crear`, para que no exista un `Usuario` sin contraseña.
2. `Usuarios/CorreoElectronico.cs` — objeto valor que valida el formato y **normaliza a minúsculas**, de modo que la unicidad no dependa de cómo se escriba.
3. `Registro/SolicitudDeRegistro.cs` — `Id`, `Correo`, `HashDelToken`, `FechaDeCreacion`, `FechaDeCaducidad`, `FechaDeConsumo`. Métodos `EstaVigente(ahora)` y `Consumir(ahora)`.
4. `Usuarios/Contrasena.cs` — objeto valor que aplica la política (longitud mínima y máxima) antes de que nadie la derive.
5. Puertos nuevos en `Puertos/`: `IRepositorioDeUsuarios`, `IRepositorioDeSolicitudesDeRegistro`, `IEnviadorDeCorreo`, `IHasheadorDeContrasenas`, `IGeneradorDeTokens`, `IEmisorDeAcceso`, `IReloj`.

### Aplicación (`Recetas.Aplicacion`)

6. `Registro/SolicitarRegistro.cs` — valida el correo; si ya hay usuario, envía aviso; si no, invalida las solicitudes vivas de ese correo, crea una nueva y envía el enlace. **Devuelve siempre el mismo resultado.**
7. `Registro/CompletarRegistro.cs` — hashea el token recibido, busca la solicitud, comprueba vigencia, crea el `Usuario`, consume la solicitud. Todos los fallos comparten un único resultado de error.
8. `Sesiones/IniciarSesion.cs` — busca el usuario, verifica la contraseña y emite el acceso. Un único error para "no existe" y "contraseña incorrecta"; **verifica un hash señuelo cuando el usuario no existe**, para que el tiempo de respuesta no delate qué correos están registrados.

### Infraestructura (`Recetas.Infraestructura`)

9. `Persistencia/Configuraciones/` — configuración EF de `Usuario` (índice único por correo) y `SolicitudDeRegistro` (índice por hash del token).
10. `Persistencia/RepositorioDeUsuariosEf.cs` y `RepositorioDeSolicitudesDeRegistroEf.cs`.
11. `Seguridad/HasheadorPbkdf2.cs` — PBKDF2-HMAC-SHA256, sal aleatoria por contraseña, alto número de iteraciones. Formato con versión (`pbkdf2-sha256$iteraciones$sal$hash`) para poder migrar el algoritmo sin romper lo ya guardado. Comparación en tiempo constante.
12. `Seguridad/GeneradorDeTokensAleatorios.cs` — 32 bytes de `RandomNumberGenerator`, codificados en base64url; el hash guardado es SHA-256 (el token ya es de alta entropía, no necesita derivación lenta).
13. `Seguridad/EmisorDeAccesoJwt.cs` — firma HMAC-SHA256 con la clave de configuración.
14. `Correo/EnviadorDeCorreoBrevo.cs` — `POST` a la API de Brevo con `HttpClient` tipado.
15. `Correo/EnviadorDeCorreoDeConsola.cs` — escribe el mensaje en el log. Es el que se registra fuera de producción.
16. `Tiempo/RelojDelSistema.cs`.
17. Migración `CuentasDeUsuario`.

### API (`Recetas.Api`)

18. `Endpoints/RegistroEndpoints.cs` y `SesionesEndpoints.cs`.
19. Autenticación JWT y un `GET /yo` protegido que devuelve la identidad: la prueba de que el token sirve.
20. Limitador de peticiones sobre el paso 1 del alta.
21. Traducción de resultados a códigos HTTP **en un único punto**, según la convención de la constitución.

### Contratos (`Recetas.Contratos`)

22. DTOs de los tres endpoints con sus validaciones, compartidos con la web.

### Web (`Recetas.Web`)

23. Proyecto Blazor WebAssembly con tres páginas y `HttpClient` apuntando a la API.
24. `wwwroot/css/tokens.css` con las variables de la constitución (color, tipografía, espaciado) y estilo aislado por página.

## Decisiones

- **`Contrasena` y `CorreoElectronico` como objetos valor** — la validación viaja con el dato. Un `string` recorriendo cuatro capas acaba usándose sin validar en la quinta.
- **PBKDF2 en lugar de Argon2** — Argon2 resiste mejor el hardware dedicado, pero exige una dependencia nativa. PBKDF2 viene en la plataforma, cumple lo que exige la constitución (derivación con sal) y el formato versionado deja la puerta abierta a migrar. Se registra como deuda consciente.
- **El hash del token es SHA-256, no PBKDF2** — un token de 256 bits aleatorios no es adivinable por fuerza bruta, así que la derivación lenta solo añadiría latencia. Las contraseñas sí la necesitan porque las eligen humanos.
- **Respuesta uniforme y verificación señuelo en el login** — un mensaje distinto, o simplemente una respuesta más rápida, convierte el endpoint en un comprobador de qué correos tienen cuenta.
- **El correo se envía dentro del caso de uso, no en segundo plano** — con una cola, un fallo de Brevo quedaría oculto y el usuario esperaría un mensaje que nunca llega. Coste: el paso 1 tarda lo que tarde Brevo. Aceptable ahora; si molesta, se revisa.
- **Enviador de consola por defecto fuera de producción** — evita el accidente clásico de mandar correo real desde la máquina de desarrollo. Brevo se activa solo con configuración explícita.
- **JWT de larga duración sin revocación** — sin tokens de refresco, una sesión corta obligaría a escribir la contraseña a diario. Se elige una caducidad amplia y se asume que no hay cierre de sesión en servidor. Anotado como límite conocido, no como olvido.
- **La web usa `localStorage` para el token** — es lo que permite mantener la sesión entre recargas en una app WebAssembly. Implica exposición a XSS: se compensa no inyectando nunca HTML sin escapar. Con `HttpOnly` no habría acceso desde WASM.

## Riesgos

- **Enumeración de cuentas por canales indirectos** — tiempos de respuesta, códigos distintos o mensajes de validación. Mitigación: respuesta uniforme, verificación señuelo y tests que comparan explícitamente las dos respuestas.
- **Fugas en los logs** — el token viaja en la URL, y registrar la petición completa lo filtraría. Mitigación: no registrar URLs completas del alta; test que comprueba que el token no aparece en la respuesta de error.
- **Brevo sin SPF/DKIM** — los correos van a spam y el alta se rompe en silencio. Mitigación: queda fuera del alcance del código pero anotado como requisito de puesta en producción.
- **Reloj y zonas horarias** — mezclar hora local y UTC produce caducidades erráticas. Mitigación: todo en UTC, forzado por el puerto `IReloj`.
- **La clave de firma del JWT se queda con un valor por defecto** — arrancar producción con una clave de ejemplo invalidaría toda la seguridad. Mitigación: la API no arranca si falta o es demasiado corta.
