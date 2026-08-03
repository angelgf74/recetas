# 003 · Recetas privadas — Tareas

## Dominio

- [x] Enumerados `TipoDePlato`, `Visibilidad` y `Unidad`.
- [x] Objeto valor `NombreDeIngrediente` con normalización.
- [x] Entidad `Ingrediente` (catálogo).
- [x] Entidad `IngredienteDeReceta` (línea con cantidad y unidad).
- [x] Raíz `Receta` con `Crear`, `Actualizar` y `EsDe`.
- [x] Puertos `IRepositorioDeRecetas` e `IRepositorioDeIngredientes`.

## Aplicación

- [x] `CrearReceta`, `ObtenerReceta`, `ListarMisRecetas`, `ActualizarReceta` y `BorrarReceta`, reunidos en `GestionDeRecetas`.
- [x] `ResolverIngredientes`, único punto de alta en el catálogo.

## Infraestructura

- [x] Configuraciones EF de las tres entidades, con cascada e índices.
- [x] `RepositorioDeRecetasEf` y `RepositorioDeIngredientesEf`.
- [x] Migración `Recetas`.

## API y contratos

- [x] DTOs de petición y respuesta.
- [x] Los cinco endpoints, todos autenticados.
- [x] Extractor del identificador de usuario desde el token, en un solo sitio.

## Validación

- [x] Tests de dominio: normalización de nombres, autoría, actualización.
- [x] Tests de aplicación de los cinco casos de uso, con sus caminos de error.
- [x] Test de que un usuario no puede leer, editar ni borrar la receta de otro.
- [x] Test de integración del ciclo completo contra PostgreSQL.
- [x] Test de que borrar una receta no deja líneas de ingrediente huérfanas.
- [x] Test de que el catálogo no duplica ingredientes con el mismo nombre normalizado.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **Los cinco casos de uso viven en una sola clase, `GestionDeRecetas`**, en lugar de cinco clases sueltas. Comparten dependencias y, sobre todo, comparten la comprobación de autoría: separarlos habría repetido cinco veces el mismo par de líneas, que es justo donde se olvida una.
- **`Unidad` no estaba en la constitución.** Se añadió como lista cerrada por coherencia con `TipoPlato`. Los valores elegidos son una propuesta: revisar si falta alguno de uso habitual.

## Notas: dos fallos que costaron encontrar

### EF no traduce `.Valor` de un objeto valor

El repositorio de ingredientes filtraba con `textos.Contains(ingrediente.Nombre.Valor)`. Compila sin problema y **falla en tiempo de ejecución** con
`The LINQ expression could not be translated`, que llegaba al cliente como un `500` sin más detalle.

La causa es que, con un conversor de valor, para EF la columna **es** el objeto valor: `.Valor` es una propiedad de C# que no existe en SQL. La forma correcta es comparar el objeto completo (`lista.Contains(ingrediente.Nombre)`), y entonces EF aplica el conversor a los parámetros. El mismo error estaba repetido en un test.

### El limitador de peticiones tumbaba los tests, sin decirlo

Seis tests de integración fallaban en grupo pero pasaban aislados. La causa era el límite de `/sesiones` (10 por ventana): esta clase abre doce sesiones y todas comparten cubo porque los tests salen del mismo origen. En el fixture se había elevado el límite de registros pero no el de inicios de sesión.

El síntoma era un token nulo dentro de cada test, lejísimos de la causa. Se añadió una comprobación explícita del estado en el helper de preparación, para que la próxima vez el fallo señale la petición que no funcionó.
