# 017 · Salud del almacenamiento

**Estado:** implementado ✅

## Qué hace

`GET /salud` deja de mirar solo la base de datos y comprueba también **el disco donde viven las fotos**: que el directorio existe, que se puede escribir en él y que queda espacio.

Si algo de eso falla, la respuesta pasa a `503` con el detalle de qué pieza está mal, igual que hoy hace con la base de datos.

## Por qué

**Hoy el disco lleno es invisible.** La API responde `200`, el monitor externo lo da por bueno y todo parece correcto, hasta que alguien intenta subir una foto y falla. El fallo se manifiesta lejos de su causa y sin nada que lo explique.

No es hipotético: ese disco tiene ahora un inquilino nuevo. Las copias de seguridad escriben ahí unos 3 MB diarios con rotación de siete días, además de los volcados de **todas** las bases del servidor. Hoy sobran 430 GB, así que no corre prisa — pero es justo la clase de fallo que aparece a los dos años, de madrugada, y cuesta una tarde diagnosticar.

**Y es lo que hace útil la vigilancia externa.** Un monitor que mira el código HTTP no puede saber nada del disco por su cuenta. Si `/salud` no lo comprueba, no lo comprueba nadie.

## Criterios de aceptación

- [x] `GET /salud` responde `200` cuando la base de datos responde **y** el almacenamiento está sano.
- [x] Responde `503` si el directorio de fotos **no existe**, **no se puede escribir** o **queda por debajo del umbral de espacio libre**.
- [x] La respuesta dice **qué pieza** está mal, no solo que algo lo está.
- [x] El umbral de espacio libre es **configurable**, con un valor por defecto razonable.
- [x] La comprobación de escritura **no deja basura**: si crea algo para probar, lo borra.
- [x] Comprobar la salud **no es caro**: nada de recorrer el directorio ni contar archivos. Lo llama un monitor cada pocos minutos.
- [x] El endpoint sigue siendo **anónimo**: es la única excepción de lectura sin sesión, y no revela nada del contenido.
- [x] Si la comprobación del disco lanza una excepción, se traduce a "degradado", **nunca a un `500`**.

## Fuera de alcance

- **Vigilar el disco de la base de datos.** Lo gestiona PostgreSQL y ya tiene sus propias herramientas; aquí se comprueba lo que este producto escribe a mano.
- **Avisar por correo desde la API.** El aviso es cosa del monitor externo, que además sigue funcionando cuando el servidor está caído del todo — que es justo cuando la API no puede avisar de nada.
- **Métricas ni histórico.** `/salud` responde sí o no, ahora. Un panel de series temporales es otro producto.
- **Comprobar Brevo.** La cuota agotada rompe las altas en silencio y merece atención, pero preguntárselo a un tercero en cada sonda es gastar cuota para vigilar la cuota. Va al backlog.
