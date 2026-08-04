# 011 · Importar receta desde URL

**Estado:** hecho

## Qué hace

El usuario pega la dirección de una receta que ha encontrado en internet y la
aplicación **rellena el formulario de receta nueva** con lo que ha podido leer de
esa página. Nada se guarda hasta que el usuario revisa y pulsa guardar.

## Por qué

Es el hueco que `mission.md` describe en su primera línea: las recetas están
dispersas en "libretas, capturas, **enlaces**". Copiar a mano una receta de una
web es tedioso y es justo el momento en que se abandona y se deja el enlace en
favoritos, que es donde se pierde.

## Encaje con la misión

`mission.md` dice que esto **no es un catálogo editorial**: sin recetas curadas
por el proyecto ni **importación masiva** de recetarios ajenos. Esta feature vive
dentro de ese límite y hay que mantenerla ahí:

- **De una en una**, y siempre porque el usuario pega una dirección concreta.
- **Sin rastreo**: no se siguen enlaces de la página ni se recorre un sitio.
- **Sin catálogo**: lo importado va al recetario privado de quien lo pide, no a
  ningún fondo común.
- **Nace privada**, como cualquier receta. Publicarla sigue siendo un acto
  explícito, y con ello el usuario asume qué está compartiendo.

## Criterios de aceptación

### Importar

- [x] `POST /recetas/importaciones` con una URL devuelve un **borrador**: nombre, ingredientes, elaboración y raciones, cuando la página los publica.
- [x] **No crea ninguna receta.** El borrador se guarda solo si el usuario envía después el formulario. Hay además un test de que el borrador se puede guardar tal cual, para que un fallo de extracción no aparezca al guardar.
- [x] Exige sesión: sin JWT, `401`.
- [x] Una página sin datos de receta reconocibles responde `422` con un mensaje que explica que hay que copiarla a mano.
- [x] Una URL mal formada responde `400`.
- [x] La respuesta incluye la dirección de origen, para que el usuario sepa de dónde salió.

### Seguridad de la petición saliente

- [x] Solo se admiten `http` y `https`. Cualquier otro esquema, `400`, y sin llegar a la red.
- [x] **No se puede alcanzar la red interna**: bucle local, privadas, enlace local, CGNAT y metadatos de nube se rechazan, incluso escritas como IPv6 (`::ffff:127.0.0.1`) o envueltas en NAT64.
- [x] La comprobación se hace **en el momento de conectar** (`ConnectCallback`), no al resolver el nombre: cierra el *DNS rebinding*.
- [x] Las redirecciones se siguen a mano con tope de saltos, y cada destino vuelve a pasar por el mismo filtro.
- [x] Hay tiempo máximo de espera y tope de bytes descargados, este último comprobado también mientras se lee, porque el `Content-Length` lo escribe el servidor de destino.
- [x] Solo se procesa HTML; otro tipo de contenido se descarta.
- [x] El error que ve el usuario **no distingue** entre "no responde", "no existe" y "es una dirección interna". Hay un test que compara los dos cuerpos byte a byte.
- [x] El endpoint tiene límite de frecuencia propio.

### Lo que se extrae

- [x] Se lee el marcado `schema.org/Recipe` en JSON-LD, suelto, dentro de una lista o dentro de un `@graph`.
- [x] Los ingredientes vienen como texto ("300 g de harina") y se parten en cantidad, unidad e ingrediente cuando se reconocen. Se entienden decimales, fracciones (`1/2` y `½`), rangos y unidades en español e inglés.
- [x] Un ingrediente que no se sabe interpretar **no se pierde**: entra con su texto como nombre y sin cantidad.
- [x] Las raciones se leen si la página las publica, y se ignoran si no encajan en el rango admitido.
- [x] El tipo de plato no se adivina: queda el valor por defecto para que lo elija el usuario.
- [x] El texto importado se recorta a los máximos del dominio en lugar de fallar.

### Web

- [x] La pantalla de receta nueva permite pegar una dirección e importar.
- [x] Tras importar, el formulario queda relleno y **editable**, con un aviso de que conviene revisarlo y de dónde viene.
- [x] Si la importación falla, se dice por qué en un idioma comprensible y el formulario sigue usable a mano.

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test por cada familia de dirección interna rechazada, y otro que las ejercita **contra la aplicación real**, sin sustituir el descargador.
- [x] Existe un test de que importar no crea ninguna receta.
- [x] Existe un test de que un ingrediente ilegible se conserva.

## Limitación conocida: hay webs que no se dejan

Probado contra tres webs de recetas reales: una respondió y **dos devolvieron `403`
o cortaron la conexión** al detectar que quien pedía no era un navegador.

No se resuelve disfrazando el cliente de navegador. Esas webs están diciendo
expresamente que no quieren que un programa lea sus páginas, y saltárselo sería
ir contra su decisión, además de una carrera que no se gana. El usuario ve el
error y copia la receta a mano, que es lo que hacía antes de esta feature.

## Fuera de alcance

- **Importar las fotos** de la página de origen. Descargar y republicar la imagen
  de un tercero es un problema distinto del texto, y con más aristas.
- **Importar varias recetas a la vez**, ni desde un índice, ni desde un archivo.
  Es exactamente la "importación masiva" que `mission.md` descarta.
- **Adivinar el tipo de plato** a partir de las categorías de la web de origen.
- **Guardar el enlace de origen** en la receta. Sería un campo nuevo; hoy la
  dirección solo se muestra durante la importación.
- **Formatos de marcado distintos de JSON-LD** (microdatos, RDFa) y páginas que
  montan la receta con JavaScript.
