# 003 · Recetas privadas

**Estado:** implementado ✅

## Qué hace

Permite a un usuario autenticado gestionar su propio recetario: crear recetas,
consultarlas, editarlas y borrarlas.

Cada receta tiene nombre, tipo de plato, lista de ingredientes con cantidad y
unidad, y elaboración. **Todas nacen privadas** y solo las ve su autor: la
publicación llega en la 005.

Es una feature de API. La interfaz llega con la 007; aquí la validación es por
tests de integración y peticiones HTTP directas.

## Por qué

Es el contenido del producto. Hasta ahora hay cuentas pero nada que guardar en
ellas, y las tres features siguientes —fotos, publicación y búsqueda— operan
todas sobre recetas ya existentes.

Hacerla privada primero no es una limitación provisional: es el orden que fija
`mission.md`. Una receta nace privada y publicarla es un acto explícito, así que
lo primero que hay que construir bien es el caso privado.

## Criterios de aceptación

### Crear

- [x] `POST /recetas` con datos válidos crea la receta y responde `201` con su ubicación.
- [x] La receta creada queda con visibilidad **privada**, sin que el cliente pueda pedir otra cosa.
- [x] El autor es siempre el usuario del token, nunca un campo de la petición.
- [x] Una receta sin nombre, sin ingredientes o sin elaboración responde `400`.
- [x] Un tipo de plato que no esté en la lista cerrada responde `400`.
- [x] Dos ingredientes con el mismo nombre en la misma receta responden `400`.
- [x] Los nombres de ingrediente se normalizan: "Tomate" y "  tomate " son el mismo ingrediente.
- [x] Sin token, `401`.

### Consultar

- [x] `GET /recetas` devuelve solo las recetas del usuario autenticado.
- [x] `GET /recetas/{id}` devuelve la receta completa con sus ingredientes.
- [x] Pedir la receta **de otro usuario** responde `404`, no `403`: un `403` confirmaría que existe.
- [x] Pedir una receta inexistente responde `404`, con el mismo cuerpo que el caso anterior.

### Editar

- [x] `PUT /recetas/{id}` actualiza nombre, tipo, elaboración e ingredientes.
- [x] Editar la receta de otro usuario responde `404` y no modifica nada.
- [x] Al editar los ingredientes, los que desaparecen dejan de estar asociados a la receta.
- [x] La edición no puede cambiar el autor ni la visibilidad.

### Borrar

- [x] `DELETE /recetas/{id}` borra la receta y responde `204`.
- [x] Borrar la receta de otro usuario responde `404` y no borra nada.
- [x] Borrar una receta arrastra sus ingredientes asociados, sin dejar filas huérfanas.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que falla si un usuario logra leer, editar o borrar la receta de otro.
- [x] El catálogo de ingredientes no duplica filas para el mismo nombre normalizado.

## Fuera de alcance

- **Fotos** — feature 004.
- **Publicar y despublicar** — feature 005. La visibilidad existe en el modelo desde la 002, pero aquí nadie puede cambiarla.
- **Búsqueda** — feature 006. Aquí el listado devuelve todas las recetas propias sin filtros.
- **Paginación y ordenación.** Un recetario personal recién creado no las necesita; cuando el listado moleste, se añaden.
- **Interfaz web** — feature 007.
- **Escalar cantidades por comensales** y **etiquetas libres** — backlog.
