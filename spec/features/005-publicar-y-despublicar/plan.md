# 005 · Publicar y despublicar — Plan

## Enfoque

Hasta ahora "puede verla" y "es suya" eran la misma pregunta, y por eso todos los
casos de uso llamaban a `Receta.EsDe`. Esta feature las separa en dos:

- **`PuedeVerla`** — es suya **o** es pública.
- **`EsDe`** — solo el autor. Sigue gobernando editar, borrar, publicar y las fotos
  de recetas privadas.

Confundirlas es exactamente el fallo que expondría recetas ajenas, así que la
distinción vive en el dominio y los casos de uso eligen explícitamente cuál usan.

La limpieza de metadatos va primero. No es una mejora que se pueda dejar para
después: en cuanto exista publicar, la primera receta compartida con foto filtra
la ubicación de su autor.

## Implementación

### Paso 0 · Limpieza de metadatos

1. Añadir `SixLabors.ImageSharp` a `Recetas.Infraestructura`.
2. `Fotos/LimpiadorDeImagenes.cs` — decodifica, aplica la orientación EXIF y vuelve a codificar sin metadatos. Se expone como puerto `ILimpiadorDeImagenes` para que el dominio no conozca la biblioteca.
3. `GestionDeFotos.SubirAsync` limpia antes de guardar.
4. Herramienta para limpiar las fotos ya subidas: un endpoint de mantenimiento no, un comando puntual sí. Se resuelve con un pequeño ejecutable de una sola vez documentado en `deploy/README.md`.

### Paso 1 · Dominio

5. `Receta.PuedeVerla(usuarioId)` — autoría **o** visibilidad pública.
6. `Receta.Publicar(ahora)` y `Receta.Despublicar(ahora)`, idempotentes.

### Paso 2 · Aplicación

7. `GestionDeRecetas.ObtenerAsync` pasa a usar `PuedeVerla`.
8. `GestionDeRecetas.PublicarAsync` y `DespublicarAsync`, que siguen exigiendo autoría.
9. `GestionDeFotos.ObtenerAsync` pasa a usar `PuedeVerla`; subir y borrar siguen exigiendo autoría.

### Paso 3 · API

10. `POST` y `DELETE` sobre `/recetas/{id}/publicacion`.
11. Repasar que la respuesta de receta no incluya nada del autor más allá de lo necesario.

## Decisiones

- **Recodificar la imagen entera en vez de recortar los segmentos EXIF.** Quitar solo el segmento APP1 sería más barato y sin dependencias, pero solo cubre JPEG y deja intactos otros contenedores de metadatos (XMP, IPTC), que también pueden llevar ubicación. Recodificar garantiza que **no queda nada**, que es la propiedad que interesa cuando el coste de equivocarse es publicar la dirección de alguien.
- **Se aplica la orientación antes de recodificar.** La orientación de la foto vive precisamente en el EXIF que se va a tirar: sin este paso, las fotos hechas en vertical se guardarían tumbadas. Es el efecto secundario que hace que "limpiar metadatos" se note en la interfaz si se hace mal.
- **La limpieza ocurre al subir, no al publicar.** Hacerlo al publicar dejaría en disco copias con ubicación mientras la receta es privada, y bastaría un fallo de permisos futuro para exponerlas. Limpiando en la puerta, el dato sensible no llega a existir en el servidor.
- **`SixLabors.ImageSharp` fijado en la 3.1, no en la última.** La **4.0 exige una clave de licencia en tiempo de compilación**: sin ella el proyecto directamente no compila (`No Six Labors license found`). La 3.1 usa la *Six Labors Split License*, gratuita para código abierto y para organizaciones por debajo de cierto volumen de facturación, y no pide clave.

  No es MIT en ninguna de las dos, así que si esto llegara a ser un producto comercial hay que revisarlo. Se anota aquí porque la constitución exige justificar cada dependencia, y porque **actualizar de la 3 a la 4 rompería la compilación**, no solo la licencia.
- **Publicar y despublicar son idempotentes.** Publicar algo ya público no es un error: el usuario pidió un estado, no una transición, y fallar solo complicaría al cliente sin proteger nada.
- **Publicar es un endpoint propio, no un campo del `PUT`.** Si la visibilidad viajara en el cuerpo de la edición, cualquier cliente que reenviara una receta entera podría publicarla sin querer. Un `POST` explícito hace que compartir sea siempre un acto deliberado, que es lo que pide `mission.md`.
- **`GET /recetas` sigue devolviendo solo las propias.** Explorar lo publicado es la 006; mezclarlo aquí convertiría el recetario personal en un muro.

## Riesgos

- **Confundir `PuedeVerla` con `EsDe`.** El riesgo central: usar la primera donde tocaba la segunda permitiría a cualquiera editar recetas públicas ajenas. Mitigación: nombres que no se parecen, y tests explícitos de editar y borrar recetas públicas de otro.
- **Fotos de recetas privadas legibles por el cambio de criterio.** Al relajar la lectura hay que revisar también el camino de las fotos. Mitigación: test de que la foto de una privada ajena sigue dando `404`.
- **Recodificar degrada la imagen.** Cada guardado con pérdida resta calidad. Mitigación: calidad alta al recodificar, y una sola recodificación por foto.
- **Fotos antiguas sin limpiar.** Las subidas antes de esta feature conservan su EXIF y se publicarían con él. Mitigación: la limpieza de lo ya existente es un criterio de aceptación, no un "ya lo haremos".
- **Coste en CPU y memoria al subir.** Decodificar una foto de móvil consume bastante más que copiar bytes. Mitigación: el límite de tamaño ya existente lo acota.
