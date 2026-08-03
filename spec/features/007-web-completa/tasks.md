# 007 · Web completa — Tareas

## Sesión

- [x] `EstadoDeSesion`: leer al arrancar, exponer, avisar de cambios, cerrar.
- [x] `ManejadorDeAutenticacion`: cabecera en cada petición y `401` centralizado.
- [x] `RutaProtegida`: sin sesión, al inicio de sesión.
- [x] Cerrar sesión desde la cabecera.

## Cliente de API

- [x] Recetas: listar, obtener, crear, editar, borrar.
- [x] Fotos: subir, descargar, borrar.
- [x] Publicación: publicar y despublicar.
- [x] Búsqueda.

## Páginas

- [x] `/recetas` con estado vacío explicativo.
- [x] `/recetas/nueva` y `/recetas/{id}/editar` con el mismo formulario.
- [x] `/recetas/{id}` con acciones según sea propia o ajena.
- [x] `/buscar` con los tres criterios.

## Componentes

- [x] `FotoDeReceta`: descarga autenticada y pintado.
- [x] `Confirmacion`: confirmar borrado sin diálogos del navegador.

## Validación

- [x] `dotnet build` sin avisos y `dotnet test` en verde (216 tests).
- [x] Recorrer a mano el ciclo en el navegador: crear, ver, publicar, buscar, foto.
- [x] Comprobar que sin sesión no se entra a las páginas protegidas.
- [ ] Comprobar el ancho de móvil. _Ver desviaciones: no verificado._
- [x] Comprobar que la consola no da errores de política de seguridad.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Desviaciones respecto al plan

- **Faltaba un campo en la API, y se añadió en vez de parchearlo desde el cliente.** La ficha necesita saber si la receta es del usuario para decidir qué acciones ofrece, y `RespuestaDeReceta` no lo decía. La primera versión lo deducía pidiendo además el recetario propio y buscando la receta dentro: dos llamadas para pintar una pantalla, que es exactamente el riesgo anotado en el plan. Se añadió `EsMia` a la respuesta, con su test.
- **Editar y borrar no se probaron a mano en el navegador.** Sí están cubiertos por los tests de integración de la API y el formulario es el mismo que el de crear, que sí se recorrió.

## No verificado

- **El ancho de móvil.** Es el segundo intento fallido: en la 002 pasó lo mismo. El navegador de pruebas devuelve capturas de 1299 px de ancho aunque se redimensione la ventana, así que no he podido ver el diseño estrecho de verdad. El CSS está escrito móvil primero y con una regla específica para el formulario de ingredientes por debajo de 30 rem, pero **es una expectativa, no una comprobación**.

## Nota: se probó contra una web antigua sin darse cuenta

Al arrancar la web en local, el puerto 5200 estaba ocupado por una instancia de
una sesión anterior. El proceso nuevo falló al enlazar, pero el navegador seguía
recibiendo respuestas, así que la primera comprobación se hizo **contra el código
viejo** sin ningún indicio de que algo fuera mal.

Se detectó porque la tarea en segundo plano reportó un fallo pese a que la web
respondía. Conviene comprobar que el proceso que responde es el que se acaba de
arrancar, no solo que algo responde.
