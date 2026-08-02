# 001 · Esqueleto y persistencia

**Estado:** implementado ✅

## Qué hace

Levanta el armazón del proyecto: una solución .NET con las capas de la arquitectura hexagonal separadas, una base de datos PostgreSQL conectada y el mecanismo de migraciones funcionando de punta a punta.

No aporta nada visible a un usuario final. Lo que entrega es la capacidad de comprobar, con un único comando, que la API arranca, alcanza la base de datos y responde. Es el suelo sobre el que se apoyan todas las features siguientes.

## Por qué

Sin esto no hay dónde colgar la primera entidad. Hacerlo como feature propia evita que la 002 (cuentas de usuario) mezcle dos problemas distintos: montar la infraestructura y resolver el alta de usuarios. Si el armazón falla, se quiere saber antes de estar depurando tokens de verificación.

También fija desde el minuto cero la regla que más cara sale de corregir tarde: **las dependencias apuntan hacia dentro**. Con los proyectos creados y referenciados correctamente, violar la arquitectura pasa a ser un error de compilación en lugar de una discusión.

## Criterios de aceptación

- [x] `dotnet build` compila la solución completa sin errores ni avisos.
- [x] El proyecto `Recetas.Dominio` **no** referencia ningún otro proyecto ni paquete de infraestructura (ni EF Core, ni ASP.NET).
- [x] Un test automático falla si alguna capa referencia a otra que no debe: la regla de dependencias está verificada, no solo documentada. _Comprobado provocando la violación a propósito; el primer enfoque no la detectaba y hubo que reforzarlo (ver `tasks.md`)._
- [x] `docker compose up -d` levanta un PostgreSQL local listo para usarse. _En el puerto 5433 del host._
- [x] `dotnet ef database update` aplica la migración inicial y crea la base de datos.
- [x] La API arranca y `GET /salud` responde `200` indicando que la conexión con PostgreSQL funciona. _Verificado: `{"estado":"correcto","baseDeDatos":true}`._
- [x] Con PostgreSQL parado, `GET /salud` responde `503` en lugar de lanzar una excepción sin controlar. _Cubierto por `SaludSinBaseDeDatosTests`, que apunta la API a un destino inalcanzable._
- [x] `dotnet test` ejecuta la suite completa en verde, incluido al menos un test de integración que arranca PostgreSQL con Testcontainers. _14 tests._
- [x] La cadena de conexión y demás secretos no están en ningún archivo versionado. _Verificado con `git check-ignore`._

## Fuera de alcance

- **Entidades de dominio.** La migración inicial no crea tablas de negocio: `Usuario` llega en la 002 y `Receta` en la 003. Aquí solo se establece el mecanismo.
- **Autenticación y JWT** — feature 002.
- **Envío de correo (Brevo)** — feature 002.
- **Cliente Blazor** — su esqueleto llega con la 002, que es quien lo necesita.
- **Despliegue al servidor.** Esta feature se valida en local; el despliegue por SSH y el túnel se abordan cuando haya algo que desplegar.
