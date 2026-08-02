# 001 · Esqueleto y persistencia — Tareas

- [x] Crear `Recetas.slnx` y los proyectos de `src/` y `tests/`.
- [x] Establecer las referencias entre proyectos según el plan.
- [x] Añadir `.gitignore`, `.editorconfig` y `git init`.
- [x] Definir el puerto `IComprobadorDeAlmacen` en `Recetas.Dominio`.
- [x] Implementar el caso de uso `ConsultarSalud` en `Recetas.Aplicacion`.
- [x] Crear `RecetasDbContext` y el adaptador `ComprobadorDeAlmacenEf` en `Recetas.Infraestructura`.
- [x] Añadir `docker-compose.yml` con PostgreSQL y volumen con nombre.
- [x] Configurar la cadena de conexión fuera del control de versiones y dejar `appsettings.Example.json`.
- [x] Generar la migración `Inicial` y aplicarla contra la base de datos local.
- [x] Exponer `GET /salud` con traducción a `200` / `503`.
- [x] Escribir los tests de arquitectura que verifican la regla de dependencias.
- [x] Escribir el test de integración de `/salud` con Testcontainers.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **La solución es `.slnx`, no `.sln`.** `dotnet new sln` genera el formato nuevo en .NET 10. Corregido en `tech-stack.md` y en el localizador de raíz de los tests de arquitectura.
- **PostgreSQL escucha en el 5433 del host**, no en el 5432: ese puerto lo ocupaba otro contenedor (`rssreader-db`) en la máquina de desarrollo. Documentado en `docker-compose.yml` y en los comandos.
- **`Recetas.Dominio.Tests` se eliminó.** El dominio todavía no tiene entidades que probar y un proyecto de test vacío solo genera ruido. Se recreará en la 002, con `Usuario`.
- **Se añadió `Recetas.Aplicacion.Tests`**, no previsto explícitamente en el plan, para probar el caso de uso `ConsultarSalud`.
- **Segunda tanda de tests de arquitectura (`ReferenciasDeProyectoTests`).** Ver la nota de abajo: la comprobación inicial resultó insuficiente.

## Nota: el test de arquitectura tenía un punto ciego

Al verificar que la regla de dependencias fallaba de verdad, se descubrió que inspeccionar el ensamblado compilado (`Assembly.GetReferencedAssemblies`) **no detecta un paquete declarado pero aún no usado**: sin uso real de un tipo, el compilador no emite la referencia en el manifiesto.

Comprobado añadiendo EF Core a `Recetas.Dominio`: con un simple `nameof(DbContext)` el test seguía en verde; solo se puso rojo al usar `typeof(DbContext)`.

Se añadió `ReferenciasDeProyectoTests`, que lee directamente los `.csproj`, de forma que la infracción salta al declarar la dependencia y no al estrenarla. Los dos enfoques se complementan y ambos se conservan.

## Mantenimiento (checklist recurrente)

- [ ] Al añadir un proyecto nuevo a la solución, comprobar que los tests de arquitectura cubren sus reglas de dependencia.
