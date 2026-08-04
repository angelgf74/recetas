# 011 · Importar receta desde URL — Tareas

## Dominio

- [x] Puerto `IDescargadorDePaginas`.
- [x] `RecetaImportada` y `LineaDeIngredienteImportada`.
- [x] `AnalizadorDeIngrediente`: texto a cantidad, unidad y nombre.
- [x] Fracciones de un carácter (`½`) y unidades inglesas, tras verlo fallar contra una web real.

## Aplicación

- [x] `LectorDeRecetaEnJsonLd`.
- [x] `ImportarReceta`, que devuelve el borrador y no persiste nada.

## Infraestructura

- [x] `ComprobadorDeDireccionesPublicas` con los rangos prohibidos.
- [x] `DescargadorDePaginasSeguro`: `ConnectCallback`, redirecciones manuales, tope de bytes y de tiempo, solo HTML.

## API

- [x] `POST /recetas/importaciones`, autenticado y con límite de frecuencia.
- [x] Contratos de petición y respuesta.

## Web

- [x] Importar desde "Nueva receta": campo, botón, aviso de revisión y de origen.
- [x] `FormularioDeReceta` acepta un borrador con el que precargarse.

## Validación

- [x] Test por cada familia de dirección interna rechazada.
- [x] Test de que un dominio público sí pasa.
- [x] Test de que importar no crea ninguna receta.
- [x] Test de extracción sobre un HTML con JSON-LD real.
- [x] Test de que un ingrediente ilegible se conserva.
- [x] Test de que una página sin receta responde `422`.
- [x] Test de que el mensaje de error es el mismo para dirección interna y para fallo de red.
- [x] Test de que el borrador se puede guardar tal cual como receta.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
