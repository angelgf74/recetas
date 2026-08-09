# 027 · Importar también la foto — Plan

## Regla que gobierna todo el diseño

**No existe un segundo cliente HTTP ni una segunda comprobación de red.** La
imagen se descarga con el mismo `HttpClient` endurecido que la página, dentro
de la misma clase `DescargadorDePaginasSeguro`. Un adaptador nuevo habría
significado reimplementar `ConnectCallback`, las redirecciones a mano y los
topes, y CLAUDE.md ya avisa de que ninguna de esas defensas se toca sin
entender cuál tapa qué: la forma de no tocarlas por accidente es no
duplicarlas.

## Dominio

`RecetaImportada` gana `UrlDeImagen` (`string?`): la URL tal como aparece en
el JSON-LD, sin descargar nada todavía. El dominio no sabe de HTTP.

`LectorDeRecetaEnJsonLd` gana `LeerImagen`, que interpreta `image` en sus
formas de `schema.org`: texto suelto, lista de textos, objeto `ImageObject`
con `url`, o lista de esos objetos. Se queda con la primera que parezca una
URL; validar que es http(s) de verdad es cosa de quien la descarga, igual que
con la dirección de la página.

## Puerto e infraestructura

`IDescargadorDePaginas` gana `DescargarImagenAsync(Uri) -> byte[]?`. Mismo
`null` único para todos los fallos, por el mismo motivo que `DescargarAsync`.

`DescargadorDePaginasSeguro`: se extrae de `LeerHtmlAsync` la parte que no
depende de HTML —el bucle de redirecciones controladas, el tope de bytes, la
lectura del flujo— a un método privado que devuelve bytes crudos. `DescargarAsync`
sigue exactamente igual por fuera (decodifica esos bytes como texto si el
`Content-Type` es HTML); `DescargarImagenAsync` es la misma ruta sin decodificar
y sin restringir por `Content-Type`, porque **no hay que fiarse de lo que
declare el servidor de origen**: el formato se decide por los bytes, en la
capa de aplicación, igual que con las fotos que sube el propio usuario.

## Aplicación

`ImportarReceta` gana `ILimpiadorDeImagenes` como dependencia. Tras leer el
borrador de texto, si `UrlDeImagen` es una URL http(s) válida:

1. `descargador.DescargarImagenAsync(uri)`.
2. `DetectorDeImagen.TryDetectar` sobre los primeros bytes — el mismo detector
   que usan las subidas, no uno nuevo.
3. `limpiador.LimpiarAsync(...)` — la misma limpieza de EXIF que las subidas.

Cualquier fallo en estos tres pasos **no aborta la importación**: el borrador
sale igual, sin foto. Es la misma filosofía que ya sigue el lector de JSON-LD
con el resto de campos: "lo que no encaja se ignora".

El tope de bytes de la imagen es el mismo que ya rige las subidas
(`OpcionesDeFotos.TamanoMaximoEnBytes`), pasado como parámetro a
`EjecutarAsync` — Aplicación no lee configuración directamente, la recibe
resuelta, igual que ya hace `GestionDeFotos.SubirAsync`.

## API

`RespuestaDeImportacion` gana `Imagen` (`byte[]?`, se serializa solo en
base64). `RecetasEndpoints.ImportarAsync` resuelve el tope de bytes desde
`IOptions<OpcionesDeFotos>` y se lo pasa a `ImportarReceta.EjecutarAsync`.

## Web

`NuevaReceta.razor` (o donde viva el flujo de importar): al recibir un
borrador con `Imagen`, la guarda en memoria. Si el usuario envía el
formulario y la receta se crea, sube esos bytes con
`Api.SubirFotoAsync(id, stream)` justo después — la misma llamada que ya usa
la ficha para subir una foto cualquiera. Si esa subida falla, no deshace la
receta ya creada: se avisa y la receta queda sin foto, para revisar a mano.

## Archivos afectados

- `RecetaImportada.cs`, `LectorDeRecetaEnJsonLd.cs`
- `IDescargadorDePaginas.cs`, `DescargadorDePaginasSeguro.cs`
- `ImportarReceta.cs`
- `ContratosDeImportacion.cs`, `RecetasEndpoints.cs`
- `ClienteDeApi.cs`, `NuevaReceta.razor`
- Tests: dominio (`LeerImagen`), aplicación (`ImportarReceta` con un limpiador
  y un descargador dobles), infraestructura (`DescargadorDePaginasSeguro`
  contra una dirección interna), integración (`ImportacionTests.cs` con el
  espía ampliado)

## Riesgos

- **Duplicar por accidente la lógica de red** si en algún momento se
  reimplementa en vez de extraer. Se comprueba con un test que llama a
  `DescargarImagenAsync` contra una dirección interna real y espera `null`,
  igual que ya se comprueba `DescargarAsync`.
- **Confiar en el `Content-Type` del servidor de origen** en vez de en los
  bytes. El diseño lo evita a propósito; el test de aplicación debe cubrir el
  caso de un servidor que miente en la cabecera.
