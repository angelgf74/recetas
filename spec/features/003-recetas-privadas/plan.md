# 003 · Recetas privadas — Plan

## Enfoque

La pieza que decide el diseño es el ingrediente. La constitución obliga a que sea
**entidad propia y no texto libre** porque la 006 buscará por él, y eso separa dos
conceptos que es fácil confundir:

- `Ingrediente` — el ingrediente en sí ("tomate"), compartido por todas las recetas.
  Es lo que hace posible preguntar "qué recetas llevan tomate".
- `IngredienteDeReceta` — su uso concreto en una receta: cantidad y unidad.

Sin esa separación, cada receta tendría su propia copia de "tomate" y la búsqueda
por ingrediente devolvería resultados incompletos según cómo lo hubiera escrito
cada usuario.

El resto sigue el patrón ya establecido: entidades y puertos en el dominio, casos
de uso que orquestan, adaptadores EF en infraestructura y endpoints que solo
traducen. La autoría se comprueba **en el dominio**, no en el endpoint.

## Implementación

### Dominio (`Recetas.Dominio`)

1. `Recetas/TipoDePlato.cs` — enumerado con los siete valores de la constitución.
2. `Recetas/Visibilidad.cs` — `Privada` | `Publica`.
3. `Recetas/Unidad.cs` — unidades de medida como lista cerrada, incluida `AlGusto`.
4. `Recetas/NombreDeIngrediente.cs` — objeto valor que normaliza (recorta, minúsculas, espacios internos colapsados). Es la clave de que el catálogo no se duplique.
5. `Recetas/Ingrediente.cs` — entidad del catálogo: `Id` y `Nombre`.
6. `Recetas/IngredienteDeReceta.cs` — la línea: referencia al ingrediente, `Cantidad` (opcional) y `Unidad`.
7. `Recetas/Receta.cs` — raíz: `Id`, `AutorId`, `Nombre`, `TipoDePlato`, `Elaboracion`, `Visibilidad`, fechas y la colección de ingredientes. Métodos `Crear`, `Actualizar` y `EsDe(autorId)`.
8. Puertos: `IRepositorioDeRecetas` e `IRepositorioDeIngredientes`.

### Aplicación (`Recetas.Aplicacion`)

9. `Recetas/CrearReceta.cs`, `ObtenerReceta.cs`, `ListarMisRecetas.cs`, `ActualizarReceta.cs`, `BorrarReceta.cs`.
10. Todos reciben el identificador del autor y **ninguno** acepta que llegue en los datos de entrada.
11. `ResolverIngredientes`: dado un conjunto de nombres, devuelve las entidades del catálogo creando las que falten. Es el punto donde se evita duplicar.

### Infraestructura (`Recetas.Infraestructura`)

12. Configuraciones EF: `Receta` (índice por autor), `Ingrediente` (índice único por nombre) e `IngredienteDeReceta` (clave compuesta, borrado en cascada desde la receta).
13. `RepositorioDeRecetasEf` y `RepositorioDeIngredientesEf`.
14. Migración `Recetas`.

### API (`Recetas.Api`)

15. `Endpoints/RecetasEndpoints.cs` con los cinco endpoints, todos bajo `RequireAuthorization`.
16. Extractor del identificador de usuario a partir del token, en un solo sitio.

### Contratos (`Recetas.Contratos`)

17. DTOs de petición y respuesta con sus validaciones.

## Decisiones

- **`NombreDeIngrediente` como objeto valor que normaliza** — la unicidad del catálogo depende de que "Tomate", "tomate" y " tomate " acaben siendo la misma cadena. Dejarlo a criterio de cada llamante garantizaría duplicados, y la búsqueda de la 006 heredaría el problema.
- **`Cantidad` opcional y unidad `AlGusto`** — "sal al gusto" es una línea de ingrediente legítima y no tiene número. Forzar un valor obligaría a inventarse un 0 que no significa nada.
- **Unidades como lista cerrada** — mismo criterio que `TipoPlato`: texto libre produciría "cucharada", "cda", "c/s" y "cucharadas" conviviendo, y cualquier agrupación futura sería imposible.
- **`Elaboracion` como un único texto, no una lista de pasos** — partir en pasos es una decisión de presentación, y la web puede hacerlo por saltos de línea. Modelarlo como lista añadiría orden, renumeración y edición parcial sin beneficio hoy. Reversible si la 007 lo pide.
- **La comprobación de autoría vive en el dominio** (`Receta.EsDe`) y los casos de uso la usan antes de devolver nada. En el endpoint sería una comprobación que se olvida en cuanto se añade el sexto endpoint.
- **"No es tuya" y "no existe" comparten camino de salida** — los casos de uso devuelven el mismo resultado, así el `404` es la consecuencia natural y no algo que cada endpoint deba recordar.
- **El catálogo de ingredientes no se limpia** — al borrar una receta desaparecen sus líneas, pero el ingrediente "tomate" permanece aunque ya no lo use nadie. Es una tabla pequeña y de solo lectura para el usuario; recolectar huérfanos añadiría trabajo sin beneficio visible.

## Riesgos

- **Fugas entre usuarios.** Es el riesgo central de la feature. Mitigación: filtrar siempre por autor en la consulta, no después en memoria, y tests explícitos de que un usuario no alcanza las recetas de otro.
- **Consultas N+1 al listar recetas con sus ingredientes.** Mitigación: carga explícita de las relaciones y, en el listado, no devolver los ingredientes completos.
- **Carrera al crear el mismo ingrediente desde dos peticiones simultáneas.** El índice único lo impediría con una excepción poco clara. Mitigación: capturar el conflicto y releer, que es lo que garantiza el índice.
- **Borrado que deja filas huérfanas.** Mitigación: cascada declarada en la configuración de EF y un test que lo comprueba contra PostgreSQL real.
