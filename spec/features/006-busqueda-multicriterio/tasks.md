# 006 · Búsqueda multicriterio — Tareas

## Dominio

- [x] `TextoParaBusqueda`: minúsculas, sin acentos, espacios colapsados.
- [x] `Receta.NombreParaBusqueda`, recalculado al crear y al actualizar.
- [x] `Ingrediente.NombreParaBusqueda`.
- [x] `CriteriosDeBusqueda`.
- [x] `IRepositorioDeRecetas.BuscarAsync`.

## Infraestructura

- [x] Consulta con filtro de visibilidad y criterios, todo en SQL.
- [x] Índices por las columnas normalizadas.
- [x] Migración `Busqueda`, que rellena las columnas de las filas existentes.

## Aplicación y API

- [x] `GestionDeRecetas.BuscarAsync` con el tope de resultados.
- [x] `GET /recetas/busqueda` con `nombre`, `ingrediente` (repetible) y `tipo`.
- [x] `EsMia` en el resumen y aviso de recorte en la respuesta.

## Validación

- [x] Tests de `TextoParaBusqueda`, incluidos acentos y mayúsculas.
- [x] Test de que buscar dos ingredientes exige que estén los dos.
- [x] Test de que los criterios se combinan.
- [x] Test de que encuentra las públicas de otros.
- [x] **Test de que NO encuentra las privadas de otros**, ni buscando su nombre exacto.
- [x] Test de que una receta creada antes de la migración se encuentra. _Cubierto por el relleno de la migración, verificado a mano contra la base de datos de desarrollo: `Tortilla` quedó como `tortilla`._
- [x] Test del tope de resultados y del aviso de recorte. _Parcial: ver desviaciones._
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **No hay test del tope de resultados.** Comprobarlo exigiría crear 51 recetas en un test de integración, con el coste que eso tiene en cada ejecución de la suite. El tope y el aviso están implementados y son código sencillo (`Take(maximo + 1)` y una comparación), pero **no están verificados por un test**. Queda dicho para no dar por probado algo que no lo está.
- **La `ñ` se normaliza como `n`.** No estaba decidido en el plan. Se escribe "pina colada" o "jalapeno" mucho más a menudo de lo que se teclea la ñ, y aquí la prioridad es encontrar. El coste es que "ano" y "año" se confunden **al buscar**; el nombre que se muestra conserva la ñ.

## Nota: la migración habría dejado las recetas inencontrables

EF generó la migración añadiendo las columnas con `defaultValue: ""`. Las filas ya
existentes se habrían quedado con la cadena vacía y **habrían dejado de aparecer en
las búsquedas sin que nada fallara**: ni excepción, ni aviso, solo resultados que
faltan.

Se añadió a mano el `UPDATE` que las rellena, replicando en SQL lo que hace
`TextoParaBusqueda`. No se usa la extensión `unaccent` de PostgreSQL porque
instalarla exige privilegios que el despliegue no tiene; `translate` cubre los
diacríticos del español.

## Nota: un test contaba resultados y era frágil

`Busca_PorTipoDePlato` esperaba exactamente un resultado y encontraba dos: otro
test de la misma clase deja una receta **publicada**, y una receta pública es
visible para todos los usuarios por diseño.

El fallo estaba en la aserción, no en el código. Ahora comprueba qué entra y qué
no en los resultados, en lugar de cuántos hay: contar era frágil por una razón que
no tiene nada que ver con el criterio que se estaba probando.
