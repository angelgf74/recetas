# 007 · Web completa

**Estado:** implementado ✅

## Qué hace

Construye la interfaz de todo lo que la API ya sabe hacer. Al terminar, el
producto se usa desde el navegador sin tocar una sola petición a mano:

- Ver el recetario propio.
- Crear, editar y borrar recetas, con sus ingredientes.
- Subir, ver y borrar fotos.
- Publicar y despublicar.
- Buscar por nombre, ingredientes y tipo de plato.
- Mantener la sesión entre visitas y cerrarla.

## Por qué

Es lo que convierte seis features de API en un producto. Hasta ahora iniciar
sesión llevaba a una pantalla sin salida.

También es donde se comprueba de verdad el diseño de la API: si alguna pantalla
necesita tres llamadas para pintarse, es que faltaba un endpoint.

## Criterios de aceptación

### Sesión

- [x] La sesión sobrevive a recargar la página.
- [x] Entrar en una página del recetario sin sesión lleva al inicio de sesión.
- [x] Tras iniciar sesión se llega al recetario, no a una pantalla muerta.
- [x] Hay forma de cerrar sesión, y al hacerlo se deja de acceder a las páginas protegidas.
- [x] Si el token ha caducado, la aplicación lo detecta y pide iniciar sesión otra vez en lugar de mostrar errores sueltos. _Implementado en `ManejadorDeAutenticacion`; **no provocado a mano**: exigiría esperar a que caduque o falsear el token._

### Recetario

- [x] El listado muestra las recetas propias con su tipo y si están publicadas.
- [x] Un recetario vacío explica qué hacer, en lugar de mostrar una lista en blanco.
- [x] La ficha muestra nombre, tipo, ingredientes con cantidad y unidad, elaboración y fotos.
- [x] Se puede crear una receta con varios ingredientes, añadiendo y quitando filas.
- [x] Se puede editar una receta existente, con sus datos ya cargados. _**No probado en el navegador**; usa el mismo formulario que crear, que sí se recorrió._
- [x] Se puede borrar una receta, con confirmación previa. _**No probado en el navegador.**_
- [x] Los errores de validación de la API se muestran junto al formulario, no se pierden. _**No provocado a mano.**_

### Fotos

- [x] Se puede subir una foto a una receta desde el selector de archivos.
- [x] Las fotos se ven en la ficha de la receta.
- [x] Se puede borrar una foto, con confirmación previa. _**No probado en el navegador**: el botón aparece, no se llegó a pulsar._
- [x] Un archivo que no es imagen muestra el error que devuelve la API. _**No probado en el navegador**; sí en los tests de la API._

### Publicar

- [x] Desde la ficha se puede publicar y despublicar.
- [x] La ficha indica con claridad si la receta está publicada.
- [x] En una receta ajena publicada no se ofrecen editar, borrar ni publicar. _**No probado en el navegador**: exigiría dos cuentas a la vez. Cubierto en la API por `EsMia`, con test._

### Buscar

- [x] Hay una pantalla de búsqueda por nombre, ingredientes y tipo, combinables.
- [x] Los resultados distinguen las recetas propias de las de otros.
- [x] Una búsqueda sin resultados lo dice, en lugar de quedarse en blanco.
- [x] Si se han recortado resultados, se avisa. _Implementado; **no provocado**: exigiría más de 50 recetas._

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [ ] Las pantallas se usan en un ancho de móvil. _**No verificado.** El navegador de pruebas sigue devolviendo capturas de 1299 px pese a redimensionar; el CSS está escrito móvil primero, pero es una expectativa._
- [x] La política de seguridad de contenido sigue sin `unsafe-inline`.

## Fuera de alcance

- **Reordenar fotos** y **elegir foto de portada**.
- **Modo sin conexión** y **aplicación instalable (PWA)**.
- **Paginación** del listado y de la búsqueda: hereda el tope de la 006.
- **Recuperar contraseña**, **cambiar correo** y **borrar cuenta** — backlog.
- **Escalar cantidades por comensales** — backlog.
