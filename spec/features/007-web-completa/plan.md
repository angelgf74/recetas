# 007 · Web completa — Plan

## Enfoque

La API está cerrada y probada, así que aquí no se decide nada de negocio: es
traducir a pantallas lo que ya existe. Las decisiones que importan son tres.

**La sesión.** Hasta ahora el token se guardaba y no se usaba para nada. Ahora
tiene que viajar en cada petición, sobrevivir a una recarga y caducar de forma
entendible. Se resuelve con un manejador de `HttpClient` que añade la cabecera y
un servicio que guarda el estado, en lugar de repetirlo en cada página.

**Las fotos.** Se sirven desde un endpoint que exige `Authorization`, y una
etiqueta `<img src="...">` no manda cabeceras. Hay que descargarlas con el cliente
autenticado y convertirlas a algo que el navegador pueda pintar.

**Las rutas protegidas.** Entrar sin sesión debe llevar al inicio de sesión, no
reventar con un `401` por pantalla.

## Implementación

### Sesión

1. `Servicios/EstadoDeSesion.cs` — lee el token de `localStorage` al arrancar, lo expone, avisa de los cambios y lo borra al cerrar sesión.
2. `Servicios/ManejadorDeAutenticacion.cs` — `DelegatingHandler` que añade `Authorization` a cada petición y, ante un `401`, cierra la sesión y lleva al inicio de sesión.
3. `Componentes/RutaProtegida.razor` — envuelve las páginas del recetario; sin sesión, redirige.

### Cliente de API

4. Ampliar `ClienteDeApi` con recetas, fotos, publicación y búsqueda. Sigue devolviendo `ResultadoDeLlamada` para que las páginas no manejen códigos HTTP.

### Páginas

5. `/recetas` — listado propio, con estado vacío explicativo.
6. `/recetas/nueva` y `/recetas/{id}/editar` — el mismo componente de formulario, con filas de ingrediente que se añaden y quitan.
7. `/recetas/{id}` — ficha: datos, ingredientes, fotos, y las acciones que correspondan según sea propia o ajena.
8. `/buscar` — los tres criterios y sus resultados.

### Componentes

9. `Componentes/FotoDeReceta.razor` — descarga la imagen autenticada y la pinta.
10. `Componentes/Confirmacion.razor` — confirmación en línea para borrar, sin diálogos del navegador.

### Estilo

11. Ampliar `tokens.css` y `app.css` con lo que pidan las pantallas nuevas, sin salirse de las variables ya definidas.

## Decisiones

- **Las fotos se pintan como `data:` y no como `blob:`.** El endpoint exige `Authorization`, así que hay que descargar los bytes con el cliente autenticado. Lo eficiente sería una URL de objeto (`blob:`), pero la política de seguridad de contenido actual permite `data:` y **no** `blob:`, y cambiarla exige tocar nginx en el servidor con `sudo`. Se paga el 33 % que engorda base64 a cambio de no depender de un paso manual de despliegue. Anotado como mejora si algún día molesta.
- **Un `DelegatingHandler` en vez de añadir la cabecera en cada llamada.** Repetirla es exactamente la clase de cosa que se olvida en el endpoint número once, y olvidarla ahí no da un error de compilación sino un `401` en producción.
- **El `401` se trata en un solo sitio.** Un token caducado no es un error de la pantalla, es un cambio de estado de la sesión: se cierra y se redirige, y ninguna página tiene que saberlo.
- **Un solo componente de formulario para crear y editar.** Los campos, la validación y el manejo de errores son idénticos; duplicarlos garantizaría que un arreglo se aplicara solo a una de las dos pantallas.
- **Confirmación en línea en lugar de `confirm()` del navegador.** Los diálogos nativos bloquean el hilo y no se pueden estilar, y en móvil se ven como una alerta del sistema ajena a la aplicación.
- **La ficha decide qué acciones ofrece con el `EsMia` que ya devuelve la API.** No hace falta comparar identificadores en el cliente, y de todas formas la autorización real está en el servidor: ocultar un botón no es una medida de seguridad, solo evita ofrecer algo que va a fallar.
- **El listado y la búsqueda son pantallas distintas.** El listado es el recetario personal; la búsqueda alcanza también lo ajeno publicado. Mezclarlas convertiría el recetario en un muro, que es lo que `mission.md` descarta.

## Riesgos

- **Que el token caduque a mitad de una sesión larga** y el usuario vea errores sueltos. Mitigación: el manejador lo detecta y redirige.
- **Fotos grandes en memoria.** Varias fotos en base64 en la misma página consumen bastante. Mitigación: el límite de 8 MB por foto ya existente; y las miniaturas siguen en el backlog.
- **Perder lo escrito al fallar el guardado.** Un error de red en un formulario largo no debe vaciar el formulario. Mitigación: los datos se mantienen y solo se muestra el error.
- **Que alguna pantalla necesite varias llamadas para pintarse**, señal de que falta un endpoint. Mitigación: si aparece, se anota; no se parchea desde el cliente encadenando peticiones.
