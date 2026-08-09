# 027 · Importar también la foto — Tareas

## Dominio

- [x] `RecetaImportada.UrlDeImagen`, `RecetaImportada.ImagenLimpia`.
- [x] `LectorDeRecetaEnJsonLd.LeerImagen` — texto, lista, `ImageObject`, lista de objetos.

## Infraestructura

- [x] `IDescargadorDePaginas.DescargarImagenAsync`.
- [x] `DescargadorDePaginasSeguro` — núcleo de descarga por bytes extraído a `DescargarBytesAsync`, reutilizado por `DescargarAsync` y `DescargarImagenAsync`. Un solo `HttpClient`, un solo `ConnectCallback`.

## Aplicación

- [x] `ImportarReceta` — descarga, detecta formato por bytes (`DetectorDeImagen`) y limpia (`ILimpiadorDeImagenes`) la imagen si la hay. Cualquier fallo deja el borrador sin foto, sin abortar la importación. Sin presupuesto (`maximoDeBytesDeImagen <= 0`), ni se intenta.

## API

- [x] `RespuestaDeImportacion.Imagen` (`byte[]?`).
- [x] `RecetasEndpoints.ImportarAsync` resuelve el tope de bytes desde `OpcionesDeFotos` y lo pasa al caso de uso.

## Web

- [x] `NuevaReceta.razor` guarda los bytes del borrador en memoria y los sube con `SubirFotoAsync` tras crear la receta.

## Pruebas

- [x] Aplicación (`LectorDeRecetaEnJsonLdTests`): `LeerImagen` en sus cuatro formas; sin imagen no rompe nada.
- [x] Aplicación (`ImportarRecetaTests`): con imagen válida, el borrador la trae ya limpia; sin presupuesto no se pide nada; fallo de descarga, formato no admitido o limpieza fallida dejan el borrador sin foto pero `Correcto`.
- [x] Infraestructura: `DescargarImagenAsync` contra direcciones internas fijas — **limitación documentada**: no distinguen nada en un entorno sin nada escuchando ahí.
- [x] Infraestructura: `DescargarImagenAsync` contra un **servidor real en bucle local, listo para responder** — el test que de verdad demuestra la protección.
- [x] Integración (`ImportacionTests`): imagen válida en el borrador; la foto se sube y queda en la receta creada; sin imagen en la página; fallo de descarga no rompe la importación; formato no admitido se ignora.

Comprobado que muerden, con sabotajes reales sobre el código:

- Quitando el filtro `maximoDeBytes <= 0` en `ImportarReceta` falla
  `SinPresupuestoParaImagen_NoIntentaDescargarla`.
- Sustituyendo `DescargarImagenAsync` por un `HttpClient` nuevo sin
  `ConnectCallback` (el descuido que este diseño existe para evitar), el test
  de arriba con direcciones fijas **sigue pasando** —confirma la limitación—,
  y `DescargarImagen_NuncaHablaConUnServidorEnBucleLocal...` **falla**, que es
  el que de verdad prueba la reutilización del cliente endurecido.

## Cierre

- [x] Suite completa en verde (691 pruebas, 0 fallos).
- [x] `CLAUDE.md`, `roadmap.md`.
- [ ] Desplegar.
- [ ] Mirarlo en pantalla si hay ocasión: sin bUnit para el flujo de subida tras importar.
