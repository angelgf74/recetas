# 019 · Exportar mis datos — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

`GET /yo/datos` devuelve el `.zip`. Cuelga de `/yo` porque es el mismo recurso que ya representa "la cuenta de quien pregunta", igual que `DELETE /yo` para la baja: el sujeto sale del token y nadie exporta lo de otro.

**El paquete se escribe directamente en el cuerpo de la respuesta**, no en memoria ni en un archivo temporal. Una cuenta con treinta fotos de 8 MB son 240 MB: cargarlos para comprimirlos tumbaría el servidor, y escribir en disco obligaría a limpiar después.

**El ZIP se arma en la capa de API**, que es donde vive la traducción a formato de salida. La aplicación entrega los datos y los flujos de las fotos; cómo se empaquetan es asunto del adaptador HTTP, igual que decidir que las recetas viajan como JSON.

## Implementación

1. **Aplicación — `Recetas.Aplicacion/Cuentas/ExportarMisDatos.cs`.** Reúne el correo y la fecha de alta del usuario y sus recetas con ingredientes y fotos. Expone además la apertura de cada foto, para que quien empaqueta no tenga que conocer el almacén.

2. **Contratos — `Recetas.Contratos/Cuentas/DatosExportados.cs`.** La forma del `datos.json`: cuenta, recetas, y en cada foto el nombre del archivo dentro del paquete. **Tipos propios, no los de la API**: el paquete es un formato que se le entrega al usuario y no debe cambiar porque cambie un endpoint.

3. **API — `SesionesEndpoints`.** `GET /yo/datos`, autenticado y con límite propio. Escribe el ZIP sobre `Response.Body`: primero `LEEME.txt`, luego `datos.json`, luego cada foto.

4. **Límite de frecuencia** — política propia, con una ventana generosa: exportar es algo que se hace una vez, y cada llamada lee todas las fotos del usuario.

5. **Web — `MiCuenta`.** Un enlace de descarga junto a la zona de baja, con la explicación de qué se lleva. **Antes** del bloque de borrar, no después: quien vaya a irse debería encontrarse primero la forma de llevarse sus cosas.

6. **Tests.** Aplicación: que reúne lo del usuario y nada ajeno. Integración: `401` sin sesión, `200` con un ZIP legible que contiene lo esperado, que una cuenta vacía produce un archivo válido, y que las fotos de otro no aparecen.

## Decisiones

- **Un `.zip` y no un JSON con las fotos en base64** — inflaría un tercio el tamaño y dejaría las fotos inservibles sin un programa que las extraiga. Con un ZIP, el usuario abre la carpeta y ve sus fotos.

- **Fotos originales, no miniaturas** — la miniatura es un detalle interno de la aplicación (009); lo que el usuario subió es lo suyo.

- **Sin el hash de la contraseña** — no le sirve a nadie para nada y es exactamente el dato que no quieres en un archivo que va a acabar en una carpeta de descargas.

- **`datos.json` con nombres en castellano**, como el resto del dominio. Lo va a abrir una persona, no un sistema.

- **Se escribe sobre la marcha (`Response.Body`)** — sin memoria intermedia ni archivos temporales que limpiar. La contrapartida es que **si algo falla a mitad, la descarga queda truncada** y no hay forma de devolver un error: las cabeceras ya se enviaron. Se acepta a cambio de no tener que gestionar temporales, y se anota abajo.

- **Sin compresión agresiva** — las fotos ya son JPEG, así que comprimirlas otra vez gasta procesador para no ganar nada. Se usa el nivel rápido.

## Riesgos

- **Descarga truncada si falla a mitad.** El usuario obtendría un ZIP corrupto sin ningún mensaje. Los ZIP con el índice al final hacen que se note al abrirlo, que es mejor que un archivo que parece bueno y no lo es. Si algún día molesta, la respuesta es la generación en segundo plano que la spec deja fuera.

- **Tiempo de respuesta con muchas fotos.** Treinta fotos son treinta lecturas de disco. Hoy no es problema —la cuenta más grande tiene un puñado—, pero es lo que hay que vigilar para decidir si algún día toca la cola de trabajos.

- **Un ZIP con las fotos de tu casa acaba en la carpeta de descargas.** No es un riesgo del servidor sino del usuario, y por eso el `LEEME.txt` lo dice con todas las letras en lugar de dar por hecho que se entiende.
