# 011 · Importar receta desde URL — Plan

## Enfoque

La feature son tres piezas, y la primera es la que importa de verdad:

1. **Traer la página sin abrir un agujero en la red interna** (SSRF).
2. **Leer la receta del HTML**, vía `schema.org/Recipe` en JSON-LD.
3. **Devolver un borrador**, no una receta.

## Lo que hace peligrosa esta feature

Hasta hoy el servidor solo hablaba con PostgreSQL y con Brevo, dos destinos
fijos. Esta feature le pide que haga una petición HTTP **a una dirección que
escribe un usuario**. Eso es falsificación de petición desde el servidor (SSRF)
en su forma más pura: quien la usa consigue que la API pida cosas desde dentro de
la máquina, saltándose el cortafuegos y `127.0.0.1`.

Aquí hay blancos reales: PostgreSQL en el puerto 5432, el propio Kestrel en
`127.0.0.1:54009`, y los servicios de metadatos de nube en `169.254.169.254`.

Defensas, todas necesarias porque cada una tapa un hueco distinto:

- **Esquema**: solo `http` y `https`. Corta `file://`, `gopher://` y compañía.
- **Comprobación de la IP en el momento de conectar**, con `ConnectCallback` de
  `SocketsHttpHandler`. Validar solo al resolver el nombre no basta: entre la
  resolución y la conexión el DNS puede devolver otra cosa (*DNS rebinding*).
  Comprobando en el `connect`, se valida exactamente la IP a la que se va.
- **Redirecciones a mano**, con tope de saltos: si se dejan en automático, el
  primer salto puede ser público y el segundo apuntar a `127.0.0.1`.
- **Tiempo máximo y tope de bytes**: sin ellos, una URL a un archivo enorme o a un
  servidor que no cierra es una denegación de servicio gratuita.
- **Solo HTML**, y descartando el resto por el tipo de contenido.
- **Respuesta indistinguible**: un solo mensaje para "no responde", "no existe" y
  "dirección interna". Si el error distinguiera, el endpoint sería un escáner de
  puertos de la red del servidor con la sesión de cualquier usuario.
- **Límite de frecuencia**, como el alta: sin él, la API es un intermediario
  anónimo para atacar a terceros.

## Implementación

### Dominio

1. Puerto `IDescargadorDePaginas`: dada una dirección, devuelve el HTML o nada. El dominio no sabe de sockets.
2. `RecetaImportada` — el borrador: nombre, elaboración, raciones y líneas de texto de ingrediente.
3. `LineaDeIngredienteImportada` / `AnalizadorDeIngrediente` — parte "300 g de harina" en cantidad, unidad y nombre. Cuando no se reconoce, **el texto entero pasa a ser el nombre**: perder un ingrediente es peor que dejarlo sin cantidad.

### Aplicación

4. `ImportarReceta` — valida la dirección, pide la página, la interpreta y devuelve el borrador. No toca el repositorio.
5. `LectorDeRecetaEnJsonLd` — busca los bloques `application/ld+json`, encuentra el objeto de tipo `Recipe` (también dentro de `@graph` o de una lista) y mapea los campos.

### Infraestructura

6. `DescargadorDePaginasSeguro` — el `HttpClient` con `ConnectCallback`, redirecciones manuales, tope de bytes y tiempo.
7. `ComprobadorDeDireccionesPublicas` — la lista de rangos prohibidos, en un tipo propio para poder probarla sola.

### API

8. `POST /recetas/importaciones`, autenticado y con límite de frecuencia.

### Web

9. En "Nueva receta", un campo para pegar la dirección y un botón de importar; el formulario se rellena con lo devuelto y queda editable, con aviso de revisión y del origen.

## Decisiones

- **Devuelve un borrador, no crea la receta.** Es lo que mantiene la feature dentro de `mission.md` ("el autor manda sobre sus datos") y lo que evita llenar recetarios de basura mal extraída. También hace que el usuario **vea** el texto ajeno que va a guardar antes de guardarlo.
- **Solo JSON-LD.** Es lo que publican prácticamente todas las webs de recetas, porque se lo pide Google. Añadir microdatos y RDFa multiplicaría el código de extracción para cubrir una minoría; si aparece la necesidad, se añade después.
- **El tipo de plato no se adivina.** Las categorías de cada web no encajan con la lista cerrada de `TipoPlato`, y colocar mal la receta es peor que dejar que el usuario elija: lo va a ver igualmente en el formulario.
- **Un ingrediente ilegible se conserva como nombre.** "Un chorrito de aceite de oliva virgen extra" no tiene cantidad reconocible, y descartarlo dejaría una receta incompleta sin decirlo.
- **El texto se recorta a los máximos del dominio**, no se rechaza. Una elaboración de 25 000 caracteres es rara pero posible, y fallar por eso obligaría al usuario a copiarla a mano igualmente.
- **No se guarda el enlace de origen.** Sería un campo nuevo en `Receta` y una migración, para un dato que hoy no se usa en ninguna pantalla.

## Riesgos

- **SSRF.** Es el riesgo central; ver arriba. Se prueba con tests por cada familia de dirección prohibida.
- **Calidad de lo extraído.** Cada web escribe el JSON-LD a su manera. Mitigación: el usuario revisa el formulario antes de guardar, así que un fallo de extracción es una molestia y no un dato corrupto.
- **Contenido de terceros.** Importar el texto de otro a un recetario privado es equivalente a copiarlo a mano. Publicarlo después ya es decisión del usuario, y la interfaz avisa de dónde viene el texto. La feature no publica nada por su cuenta.
- **Peticiones salientes desde el servidor.** El servidor pasa a generar tráfico a destinos que elige un usuario. Mitigado con el límite de frecuencia y el tiempo máximo.
