# 024 · Etiquetas libres — Plan

## Enfoque

Calco exacto del catálogo de ingredientes (003/006), con una diferencia: una
etiqueta no lleva cantidad ni unidad, así que la relación con la receta es
membresía pura, no una línea con datos propios.

- `Etiqueta` — catálogo compartido, como `Ingrediente`. Un nombre, normalizado.
- `EtiquetaDeReceta` — el vínculo, como `IngredienteDeReceta` pero sin
  `Cantidad`/`Unidad`: solo `RecetaId` + `EtiquetaId`.
- `ResolverEtiquetas` — como `ResolverIngredientes`: traduce texto libre a
  identificadores del catálogo, creando lo que no exista.
- Búsqueda: mismo patrón que el filtro por ingredientes en
  `RepositorioDeRecetasEf.BuscarAsync`, un `Where...Any` más.

**Por qué un entidad de vínculo explícita y no un many-to-many implícito de
EF.** Sería menos código, pero el proyecto ya se ha encontrado con la trampa
de EF que documenta `CLAUDE.md`: una entidad con `Guid` puesto por el dominio
que cuelga de un padre ya rastreado puede acabar generando un `UPDATE` donde
tocaba un `INSERT`. `IngredienteDeReceta` la evita porque su clave es
compuesta (`RecetaId` + `IngredienteId`), no un `Guid` autogenerado, y porque
las filas se crean explícitamente. Un many-to-many implícito reintroduciría
exactamente el terreno donde esa trampa vive, por una feature que no lo
necesita: replicar la forma que ya funciona es más seguro que ahorrarse una
clase.

## Dominio

`NombreDeEtiqueta` — value object, calco de `NombreDeIngrediente`: recorta,
colapsa espacios, pasa a minúsculas. **No quita acentos**, por el mismo motivo
que los ingredientes: en español distinguen palabras. Tope de longitud más
corto que el de ingrediente —40 caracteres— porque una etiqueta es una palabra
o dos, no una descripción.

`Etiqueta` — catálogo: `Id`, `Nombre` (`NombreDeEtiqueta`), `NombreParaBusqueda`
(`TextoParaBusqueda.Normalizar`, sin acentos, para que la búsqueda funcione
igual que con el nombre de la receta y los ingredientes).

`EtiquetaDeReceta` — `RecetaId`, `EtiquetaId`, navegación `Etiqueta?` para la
carga con `Include`. Clave compuesta.

`Receta`:
- `_etiquetas: List<EtiquetaDeReceta>`, expuesta como `Etiquetas`.
- `Receta.MaximoDeEtiquetas = 10` — tope duro. Sin él, el campo se convierte en
  una forma de meter texto libre sin límite en una receta, que es justo lo que
  la elaboración ya cubre con su propio tope.
- `ReemplazarEtiquetas(IEnumerable<Guid> etiquetaIds)` — a diferencia de
  `ReemplazarIngredientes`, **una lista vacía es válida**: las etiquetas no son
  obligatorias. Sí se rechazan duplicados y pasarse del tope.

## Aplicación

`ResolverEtiquetas(IRepositorioDeEtiquetas)` — normaliza, deduplica, resuelve
contra el catálogo y crea lo que falte, igual que `ResolverIngredientes`.
Devuelve `null` si algún nombre no es válido o se supera el tope: mismo
contrato que su equivalente, para que `GestionDeRecetas` lo trate como
"datos no válidos" sin lógica nueva.

`DatosDeReceta` gana `Etiquetas` (`IReadOnlyCollection<string>`, por defecto
vacía) como último parámetro. `GestionDeRecetas.CrearAsync` y `ActualizarAsync`
resuelven las etiquetas igual que ya resuelven los ingredientes, antes de
tocar el dominio.

`CriteriosDeBusqueda` gana `Etiquetas` (`IReadOnlyCollection<string>`, ya
normalizadas), con la misma semántica **Y** que `Ingredientes` y la misma
razón: afinar una búsqueda tiene que devolver menos resultados, no más.

## Infraestructura

`ConfiguracionDeEtiqueta` y `ConfiguracionDeEtiquetaDeReceta`, calco de las de
ingrediente: índice único sobre `Nombre`, índice no único sobre
`NombreParaBusqueda`, clave compuesta en el vínculo, `OnDelete(Restrict)` desde
`EtiquetaDeReceta` hacia `Etiqueta` (el catálogo no se borra por editar una
receta) y `OnDelete(Cascade)` hacia `Receta` (borrar la receta se lleva sus
vínculos, no el catálogo).

`RepositorioDeEtiquetasEf` — calco de `RepositorioDeIngredientesEf`:
`BuscarPorNombresAsync`, `AnadirNuevosAsync`.

`RepositorioDeRecetasEf.BuscarAsync` gana un `Where` más, con la misma forma
que el de ingredientes: `receta.Etiquetas.Any(vinculo =>
vinculo.Etiqueta!.NombreParaBusqueda.Contains(buscado))` por cada etiqueta
pedida. Va **dentro** de la consulta con el resto de filtros, antes del
`Include` final, siguiendo la regla de "el filtro de visibilidad y los
criterios se aplican en la base de datos, nunca después en memoria".

Migración nueva: tablas `etiquetas` y `etiquetas_de_receta`.

## API

`PeticionDeReceta.Etiquetas` — `List<string>`, `[MaxLength(10)]` sobre la
colección (repite el tope del dominio, como ya hace `RacionesMinimas`/
`RacionesMaximas`: Contratos no referencia Dominio).

`RespuestaDeReceta.Etiquetas` — `IReadOnlyCollection<string>`, los nombres tal
como están en el catálogo (ya en minúsculas, igual que los ingredientes en la
respuesta actual).

**No entra en `ResumenDeReceta`.** Mismo argumento que `EsFavorita` en la 021:
el listado no lo necesita, añadirlo costaría una consulta más por cada receta
del listado, y si se echa en falta se añade entonces.

`GET /recetas/busqueda` acepta `?etiqueta=` repetido, igual que `?ingrediente=`.

## Web

- `FormularioDeReceta.razor` — un campo de texto "Etiquetas, separadas por
  comas", igual que el campo de ingredientes de la búsqueda: sin fila por
  fila, porque una etiqueta no tiene más dato que su propio texto.
- `FichaDeReceta.razor` — las etiquetas se muestran junto al tipo de plato,
  como texto simple (sin quitar la simplicidad de la ficha por una feature
  que es un complemento).
- `Buscar.razor` — un campo más, mismo patrón que el de ingredientes.
- `ClienteDeApi.BuscarAsync` gana el parámetro `etiquetas`.

## Pasos

1. Dominio: `NombreDeEtiqueta`, `Etiqueta`, `EtiquetaDeReceta`, cambios en `Receta`.
2. `ResolverEtiquetas`, cambios en `DatosDeReceta` y `GestionDeRecetas`.
3. `CriteriosDeBusqueda.Etiquetas`.
4. Infraestructura: configuraciones, repositorio, migración, filtro de búsqueda.
5. Contratos y endpoint.
6. Web: formulario, ficha, búsqueda, cliente.
7. Tests.

## Archivos afectados

**Nuevos**

- `src/Recetas.Dominio/Recetas/NombreDeEtiqueta.cs`
- `src/Recetas.Dominio/Recetas/Etiqueta.cs`
- `src/Recetas.Dominio/Recetas/EtiquetaDeReceta.cs`
- `src/Recetas.Dominio/Puertos/IRepositorioDeEtiquetas.cs`
- `src/Recetas.Aplicacion/Recetas/ResolverEtiquetas.cs`
- `src/Recetas.Infraestructura/Persistencia/RepositorioDeEtiquetasEf.cs`
- `src/Recetas.Infraestructura/Persistencia/Configuraciones/ConfiguracionDeEtiqueta.cs`
- Migración
- Tests correspondientes en los tres proyectos de test

**Modificados**

- `Receta.cs`, `GestionDeRecetas.cs`, `CriteriosDeBusqueda.cs`
- `RecetasDbContext.cs`, `RepositorioDeRecetasEf.cs`, `InyeccionDeDependencias.cs` (aplicación e infraestructura)
- `ContratosDeRecetas.cs`, `RecetasEndpoints.cs`
- `FormularioDeReceta.razor`, `FichaDeReceta.razor`, `Buscar.razor`, `ClienteDeApi.cs`
- Dobles de test: repositorio de etiquetas en memoria, y el de recetas en memoria para reproducir el filtro

## Riesgos y decisiones

- **El choque de nombres con la clase de ayuda `Recetas.Web.Etiquetas`** (textos
  de enumerado: `DeTipo`, `DeUnidad`, `DeCantidad`) se resolvió antes de tocar
  esta feature, renombrándola a `TextosDeEnumerado`. No tiene relación con
  "etiquetar recetas"; el nombre solo coincidía por casualidad.
- **Etiquetas privadas de otro no se filtran** porque la búsqueda ya parte de
  `PuedeVerla`: el filtro por etiqueta se añade *después* de esa condición, así
  que nunca se pregunta por etiquetas de recetas que no se podrían ver. Va con
  test explícito, no solo por inspección.
- **El tope de 10 por receta** es una elección, no una medida de nada
  concreto: suficiente para "sin gluten, rápido, de la abuela" varias veces,
  corto para que nadie use el campo como una segunda elaboración.
