# 006 · Búsqueda multicriterio — Plan

## Enfoque

Dos problemas que parecen uno.

El primero es **acentos y mayúsculas**. En español se busca "jamon" y se espera
encontrar "Jamón". La solución habitual en PostgreSQL es la extensión `unaccent`,
pero instalarla exige privilegios sobre la base de datos que el despliegue no
tiene. Se resuelve dentro de la aplicación: junto al nombre real se guarda una
**columna normalizada** —minúsculas y sin acentos— y las búsquedas van contra
ella. El nombre que se muestra no cambia.

El segundo es **la visibilidad**. Buscar es la primera operación que mira muchas
recetas de golpe, y la primera donde un filtro mal puesto no da un error sino un
resultado de más. El filtro se aplica **en la consulta**, no descartando después
en memoria: así no existe el momento en el que una receta ajena está cargada y
solo una condición la separa de la respuesta.

## Implementación

### Dominio

1. `Recetas/TextoParaBusqueda.cs` — normaliza: recorta, colapsa espacios, pasa a minúsculas y quita los diacríticos descomponiendo en Unicode.
2. `Receta.NombreParaBusqueda`, recalculado en `Crear` y `Actualizar`.
3. `Ingrediente.NombreParaBusqueda`, calculado al crear.
4. `Recetas/CriteriosDeBusqueda.cs` — objeto con nombre, ingredientes y tipo, todos opcionales.
5. Ampliar `IRepositorioDeRecetas` con `BuscarAsync(criterios, usuarioId)`.

### Infraestructura

6. `RepositorioDeRecetasEf.BuscarAsync` — una sola consulta con el filtro de visibilidad y los criterios, más el tope de resultados.
7. Índices por las columnas normalizadas.
8. Migración `Busqueda`, que además **rellena las columnas nuevas** de las filas ya existentes.

### Aplicación

9. `GestionDeRecetas.BuscarAsync`, que traduce los criterios y aplica el tope.

### API y contratos

10. `GET /recetas/busqueda` con `nombre`, `ingrediente` (repetible) y `tipo`.
11. `ResumenDeReceta` gana `EsMia`; la respuesta de búsqueda añade si se recortó.

## Decisiones

- **Columna normalizada en lugar de la extensión `unaccent`.** `unaccent` sería más elegante y evitaría duplicar el dato, pero `CREATE EXTENSION` necesita privilegios que el usuario `webapps` no tiene, y el despliegue es por migraciones sin acceso administrativo. Una columna calculada por la aplicación funciona con los permisos que ya hay y se prueba sin base de datos.
- **Los acentos se quitan solo para buscar.** El nombre canónico los conserva, como ya decidió la 003 para los ingredientes: en español distinguen palabras y "anís" no debe convertirse en "anis" al mostrarlo. La normalización es una vista para consultar, no el dato.
- **Varios ingredientes se combinan con Y, no con O.** "Encuéntrame algo con tomate y albahaca" significa las dos cosas. Con O, cuantos más ingredientes se indicaran más resultados saldrían, que es lo contrario de lo que espera quien está afinando una búsqueda.
- **El filtro de visibilidad va dentro de la consulta.** Cargar y descartar después funcionaría igual mientras el código sea correcto, pero deja abierto que una refactorización futura devuelva la lista antes de filtrar. En la consulta, la receta ajena no llega a salir de la base de datos.
- **Tope de resultados en lugar de paginación.** Devolver todo sin límite es un riesgo real; recorrer páginas todavía no le hace falta a nadie. Se acota y se avisa de que se ha recortado, que es la mitad barata del problema. La paginación entra cuando el tope moleste.
- **Buscar es un endpoint aparte de `GET /recetas`.** El listado es el recetario personal y la búsqueda alcanza también lo ajeno publicado: mezclarlos convertiría el recetario en un muro, que es justo lo que `mission.md` descarta.
- **Cada resultado dice si es propio, pero no de quién es.** Saber si una receta es tuya es necesario para la interfaz; saber de quién es la ajena no aporta nada y expondría datos de otro usuario.

## Riesgos

- **Que una receta privada ajena se cuele.** El riesgo central. Mitigación: filtro en la consulta y un test que crea una receta privada con un nombre único y comprueba que otro usuario no la encuentra buscándolo exactamente.
- **Rellenar mal las columnas nuevas al migrar.** Si las filas existentes quedan con la columna vacía, esas recetas se vuelven inencontrables sin que nada falle. Mitigación: la migración las rellena y hay un test que busca una receta creada antes del cambio.
- **Normalización distinta al guardar y al buscar.** Si el texto de la consulta se normaliza con reglas distintas al de la columna, no casa nunca. Mitigación: una única función, usada en ambos lados.
- **Consultas lentas al crecer.** `LIKE '%texto%'` no usa índice por el comodín inicial. Mitigación: a esta escala no importa; anotado por si algún día importa.
