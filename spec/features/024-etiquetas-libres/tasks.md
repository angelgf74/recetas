# 024 · Etiquetas libres — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `NombreDeEtiqueta` — normaliza, sin acentos fuera, tope de 40 caracteres.
- [x] `Etiqueta` — catálogo compartido.
- [x] `EtiquetaDeReceta` — vínculo, clave compuesta.
- [x] `IRepositorioDeEtiquetas` — `BuscarPorNombresAsync`, `AnadirNuevasAsync`.
- [x] `Receta.MaximoDeEtiquetas`, `Receta.Etiquetas`, `Receta.ReemplazarEtiquetas` (lista vacía válida, duplicados y exceso de tope rechazados).

## Aplicación

- [x] `ResolverEtiquetas`.
- [x] `DatosDeReceta.Etiquetas`; `GestionDeRecetas.CrearAsync`/`ActualizarAsync` las resuelven.
- [x] `CriteriosDeBusqueda.Etiquetas`, normalizadas en `CriteriosDeBusqueda.Crear`.
- [x] Registrar `ResolverEtiquetas` en la inyección de dependencias.

## Infraestructura

- [x] `ConfiguracionDeEtiqueta` y `ConfiguracionDeEtiquetaDeReceta`.
- [x] `DbSet<Etiqueta>` en el contexto.
- [x] `RepositorioDeEtiquetasEf`.
- [x] Filtro por etiqueta en `RepositorioDeRecetasEf.BuscarAsync`, dentro de la consulta.
- [x] Migración `20260809135101_Etiquetas`.

## API

- [x] `PeticionDeReceta.Etiquetas` con tope de colección.
- [x] `RespuestaDeReceta.Etiquetas`.
- [x] `GET /recetas/busqueda?etiqueta=` repetido.
- [x] `TryTraducir` en `RecetasEndpoints` traslada las etiquetas a `DatosDeReceta`.

## Web

- [x] Campo de etiquetas en `FormularioDeReceta.razor`.
- [x] Etiquetas visibles en `FichaDeReceta.razor`.
- [x] Campo de etiquetas en `Buscar.razor`.
- [x] `ClienteDeApi.BuscarAsync` con el parámetro nuevo.

## Pruebas

- [x] Dominio: `NombreDeEtiqueta` normaliza igual que `NombreDeIngrediente`, no quita acentos.
- [x] Dominio: `ReemplazarEtiquetas` con lista vacía es válido.
- [x] Dominio: `ReemplazarEtiquetas` rechaza duplicados y más del tope.
- [x] Aplicación: crear con etiquetas nuevas las da de alta en el catálogo.
- [x] Aplicación: reutilizar una etiqueta existente no crea una fila nueva.
- [x] Aplicación: pasarse del tope rechaza toda la receta.
- [x] Aplicación: editar puede quitar todas las etiquetas.
- [x] Integración: buscar por una etiqueta encuentra recetas que la llevan.
- [x] Integración: buscar por varias etiquetas exige todas.
- [x] Integración: **una etiqueta de una receta privada ajena no aparece** en la búsqueda de otro usuario.
- [x] Integración: crear y editar una receta con etiquetas, ida y vuelta por la API.
- [x] Integración: pasarse del tope responde `400`.

Comprobado que muerden: desactivando el filtro de etiquetas en
`RepositorioDeRecetasEf.BuscarAsync` fallan `Busca_PorEtiquetaSinAcentos` y
`VariasEtiquetas_ExigenQueEstenTodas`; el test de privacidad
(`Etiquetas_DeUnaPrivadaAjena_NoFiltranNada`) sigue en verde porque comprueba
el filtro de **visibilidad**, no el de etiqueta, que sigue intacto — confirma
que son dos guardas independientes, no una sola.

**Sin bUnit** para el campo del formulario ni el de búsqueda: no lo pedía el
plan y no había tiempo en esta pasada. Anotado en `spec.md`.

## Cierre

- [x] Suite completa en verde (643 pruebas, 0 fallos).
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Actualizar la lista de endpoints en `CLAUDE.md`.
- [x] Quitar la entrada del backlog en `roadmap.md` y añadir la feature a "Hecho".
- [ ] Desplegar.
- [ ] Mirarlo en pantalla si hay ocasión.

## Mantenimiento (checklist recurrente)

- [ ] Si algún día se añade un contador de uso de etiquetas o un ranking, releer la decisión de `mission.md` sobre por qué los favoritos no cuentan nada: es el mismo argumento.
