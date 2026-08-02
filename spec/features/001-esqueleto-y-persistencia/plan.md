# 001 · Esqueleto y persistencia — Plan

## Enfoque

Un *walking skeleton*: el camino completo desde una petición HTTP hasta PostgreSQL y de vuelta, con todas las capas presentes pero sin lógica de negocio dentro. El endpoint `GET /salud` recorre ese camino entero, así que si algo del montaje está mal, se sabe en la primera ejecución y no tres features más tarde.

Las capas se crean como proyectos .NET separados en lugar de carpetas dentro de un proyecto único. Es lo que convierte la regla de dependencias de la constitución en algo que el compilador vigila: `Recetas.Dominio` no puede importar EF Core si nadie le da esa referencia.

## Implementación

1. **Solución y proyectos** — `Recetas.sln` con seis proyectos bajo `src/`: `Dominio` (biblioteca sin dependencias), `Aplicacion`, `Infraestructura`, `Contratos`, `Api` (Web API) y, en `tests/`, los proyectos de prueba.
2. **Referencias entre proyectos** — `Api` → `Aplicacion` + `Contratos` + `Infraestructura` (solo para componer dependencias en el arranque); `Aplicacion` → `Dominio`; `Infraestructura` → `Dominio`; `Contratos` sin referencias. `Dominio` sin ninguna.
3. **Puerto de salud en el dominio** — `Recetas.Dominio/Puertos/IComprobadorDeAlmacen.cs`: una operación que responde si el almacén responde. Deliberadamente sin mencionar bases de datos: el dominio no sabe que hay PostgreSQL detrás.
4. **Caso de uso** — `Recetas.Aplicacion/Salud/ConsultarSalud.cs`, que usa el puerto y devuelve el estado. Trivial, pero establece el patrón que seguirán los casos de uso reales.
5. **Persistencia** — `Recetas.Infraestructura/Persistencia/RecetasDbContext.cs` con Npgsql, más el adaptador `ComprobadorDeAlmacenEf` que implementa el puerto contra la base de datos.
6. **Migración inicial** — `dotnet ef migrations add Inicial`. Sin tablas de negocio: crea el historial de migraciones y demuestra que la cadena de herramientas funciona.
7. **Endpoint** — `GET /salud` en `Recetas.Api`, que invoca el caso de uso y traduce el resultado a `200` o `503`.
8. **Configuración** — cadena de conexión por `appsettings.Development.json` (ignorado por git) y variables de entorno. En el repositorio solo queda `appsettings.Example.json` con valores de ejemplo.
9. **PostgreSQL local** — `docker-compose.yml` con el servicio de base de datos y un volumen con nombre para que los datos sobrevivan a un reinicio.
10. **Tests de arquitectura** — `tests/Recetas.Arquitectura.Tests`: comprueban por reflexión que `Recetas.Dominio` no depende de EF ni de ASP.NET, y que las capas respetan el sentido de las flechas.
11. **Tests de integración** — `tests/Recetas.Api.Tests`: levantan PostgreSQL con Testcontainers, aplican las migraciones y comprueban que `/salud` responde `200`.
12. **Higiene del repositorio** — `git init`, `.gitignore` de .NET (con los `appsettings.Development.json` incluidos) y `.editorconfig` con las convenciones de la constitución.

## Decisiones

- **La migración inicial no crea tablas de negocio** — se descartó adelantar la entidad `Usuario` para "tener algo que migrar". Habría metido la 002 dentro de la 001 y roto el orden del roadmap. Una migración vacía sigue demostrando lo que interesa: que las herramientas, la cadena de conexión y el historial funcionan.
- **`Contratos` no referencia a `Dominio`** — es el contrato público hacia los clientes, y si arrastrase el dominio, cualquier cambio interno se filtraría a la API pública. La conversión entre ambos vive en `Aplicacion`.
- **La regla de dependencias se testea, no solo se documenta** — una regla que nadie comprueba se incumple en cuanto aprieta la prisa. Convertirla en un test rojo es lo que la sostiene.
- **`Api` referencia a `Infraestructura`** — es la excepción consciente a "las flechas apuntan hacia dentro": alguien tiene que instanciar los adaptadores concretos al arrancar. Se limita al archivo de composición de dependencias; ningún endpoint usa tipos de infraestructura directamente, y el test de arquitectura lo vigila.
- **Endpoint `/salud` en lugar de una simple prueba de compilación** — comprobar que compila no demuestra que la base de datos esté conectada. El endpoint recorre el camino real.
- **Testcontainers en vez de una base de datos compartida de pruebas** — cada ejecución arranca limpia y los tests no dependen del estado que dejó la anterior. Coste: Docker es obligatorio para pasar la suite completa.

## Riesgos

- **Docker no disponible al ejecutar los tests** — los de integración fallarían por entorno, no por código. Mitigación: separarlos por categoría para poder correr la suite unitaria sin Docker.
- **Una migración vacía puede parecer un error** — quien llegue después puede pensar que falta algo. Mitigación: queda explicado aquí y en el propio nombre de la migración.
- **La cadena de conexión se cuela en el repositorio** — es el fallo clásico del primer día. Mitigación: `.gitignore` desde el primer commit y solo un archivo de ejemplo versionado.
