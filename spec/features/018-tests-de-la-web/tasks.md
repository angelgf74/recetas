# 018 · Tests de la web — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Antes de nada

- [x] Comprobar que **bUnit restaura con .NET 10**. Si no, esta feature cambia de forma y hay que replantear el plan.

## Proyecto

- [x] `tests/Recetas.Web.Tests` con xUnit y bUnit, referenciando `Recetas.Web`.
- [x] Añadirlo a `Recetas.slnx`.
- [x] Extender los **dos** enfoques de `Recetas.Arquitectura.Tests` con el proyecto nuevo.

## Andamiaje

- [x] `ManejadorDeRespuestas` — respuestas HTTP preparadas por ruta, sin red.
- [x] `ContextoDeWeb` — base con `ClienteDeApi`, `EstadoDeSesion` y los servicios que pidan los componentes.

## Tests

- [x] `Confirmacion`: no invoca la acción hasta confirmar; cancelar no la invoca.
- [x] Ficha de receta **propia**: ofrece editar, foto, compartir y borrar; no ofrece denunciar.
- [x] Ficha de receta **ajena**: ofrece denunciar; no ofrece las acciones de autor.
- [x] Ficha ajena **con permiso de retirada**: ofrece retirar. ← el que habría cazado el fallo de la 015.
- [x] Ficha ajena **sin** permiso de retirada: no lo ofrece.
- [x] Ficha ajena **ya denunciada**: deja de ofrecer denunciar.

## Cierre

- [x] **Romper la web a propósito y ver `dotnet test` en rojo.** Sin esta comprobación, la mitad de la feature no está demostrada.
- [x] `dotnet format` sin avisos nuevos y suite completa en verde.
- [x] Actualizar `tech-stack.md`: la web ya tiene tests, y ya entra en `dotnet test`.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Mantenimiento (checklist recurrente)

- [ ] Al añadir a un componente una condición que decide **qué acciones se ofrecen**, añadir su test aquí. Es la clase de fallo que la API no puede detectar: allí el endpoint existe y responde bien; lo que falta es la forma de llegar a él.
