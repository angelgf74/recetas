# 009 · Fotos en los listados — Plan

## Enfoque

La miniatura no es una entidad nueva: es **otra representación del mismo
`Foto`**. No lleva fila propia, ni identificador, ni fecha. Su ruta se deriva del
identificador de la foto igual que la del original, con un sufijo. Así no hay
migración de base de datos, y una foto y su miniatura no pueden desincronizarse
por un error de escritura en dos tablas.

El escalado va donde ya está ImageSharp, detrás de un puerto propio. El
`ILimpiadorDeImagenes` no se amplía: limpiar metadatos y redimensionar son dos
cosas distintas, y meterlas en el mismo puerto obligaría a que cualquier
implementación futura hiciera las dos.

**La miniatura se genera a partir de la imagen ya limpia**, nunca del archivo que
subió el cliente. Si saliera del original, un fallo de orientación o un metadato
colado se llevaría también a la miniatura, que es la copia que más se va a ver.

## Implementación

### Dominio

1. Puerto `IEscaladorDeImagenes`: `Task<Stream?> EscalarAsync(Stream, TipoDeImagen, int anchoMaximo, ...)`.
2. `IAlmacenDeFotos` gana `GuardarMiniaturaAsync`, `AbrirMiniaturaAsync` y el borrado de la miniatura dentro de `BorrarAsync`. Sigue hablando de identificadores, nunca de rutas.
3. `Receta.FotoDePortada` — propiedad calculada: la foto de menor `FechaDeSubida`, o `null`. No es un campo: derivarla evita un dato que pueda quedar apuntando a una foto borrada.

### Aplicación

4. `GestionDeFotos.SubirAsync` genera y guarda la miniatura junto al original.
5. `GestionDeFotos.ObtenerMiniaturaAsync` — mismas comprobaciones de visibilidad que `ObtenerAsync`. Si la miniatura no está en disco, la genera desde el original y la guarda antes de devolverla.

### Infraestructura

6. `EscaladorDeImagenesConImageSharp`.
7. `AlmacenDeFotosEnDisco`: rutas `{id}.{ext}` y `{id}-min.{ext}`.

### API

8. `GET /recetas/{recetaId}/fotos/{fotoId}/miniatura`, dentro del grupo que ya exige autorización.
9. `ResumenDeReceta` gana `Guid? FotoDePortadaId`.

### Web

10. `FotoDeReceta` gana un parámetro para pedir la miniatura en vez del original.
11. El recetario y la búsqueda pintan la portada; hueco reservado cuando no hay foto.

## Decisiones

- **La miniatura conserva el formato del original.** Reconvertir todo a JPEG pesaría menos, pero obliga a decidir qué hacer con la transparencia de un PNG —aplanarla contra un fondo que la aplicación no controla— y hace que el tipo que devuelve el servidor no sea el que subió el usuario. Conservarlo reutiliza `TipoDeImagen` entero: extensión, tipo de contenido y codificador. Si el peso de los PNG llega a molestar, es un cambio de una línea en el codificador.
- **Ancho máximo, sin recortar.** Recortar a cuadrado daría tarjetas más regulares, pero decide por el usuario qué parte de su foto importa, y en un plato emplatado la respuesta rara vez es "el centro".
- **Nunca se amplía.** Una foto ya pequeña se guarda tal cual: ampliarla la haría pesar más y verse peor, que es lo contrario de lo que hace una miniatura.
- **Generación perezosa para lo ya subido.** Las fotos que ya están en producción no tienen miniatura. En vez de un script de relleno —que hay que escribir, probar y ejecutar a mano en el servidor— se genera la primera vez que se pide y se guarda. El coste es una petición lenta por foto antigua, una sola vez.
- **Portada derivada, no almacenada.** La foto más antigua. Un campo `PortadaId` habría que mantenerlo al borrar esa foto, y olvidarlo dejaría listados con imágenes rotas.
- **La miniatura sale de la imagen limpia.** Ver arriba: es la copia que más se ve.
- **Mismo endpoint autenticado, mismas reglas.** La miniatura de una receta privada es tan privada como la foto: se resuelve con `PuedeVerla`, igual que la descarga completa, y responde `404` cuando no toca.

## Riesgos

- **Cincuenta tarjetas son cincuenta peticiones**, cada una con su base64. Mitigación: la miniatura se acota a un ancho pequeño, con lo que el total queda en unos pocos megabytes en el caso peor —una búsqueda tope—, y el navegador las encola de seis en seis. Si molesta, la salida no es agrandar la miniatura sino paginar, que ya está anotado en `GestionDeRecetas.MaximoDeResultados`.
- **Foto sin miniatura y sin original.** Si el original falta, no hay nada de lo que escalar: se responde `404`, igual que hoy hace la descarga completa.
- **Escalar en la petición.** La generación perezosa hace trabajo de CPU dentro de una petición GET. Solo ocurre una vez por foto antigua, y el tamaño de entrada está acotado por el límite de subida.
