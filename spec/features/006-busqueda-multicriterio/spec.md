# 006 · Búsqueda multicriterio

**Estado:** implementado ✅

## Qué hace

Permite encontrar recetas por **nombre**, por **ingredientes** y por **tipo de
plato**, combinando los criterios que se quiera.

Busca en el recetario propio —privadas y públicas— y en las recetas **publicadas
por otros**. Nunca en las privadas ajenas.

Es una feature de API. La interfaz llega con la 007.

## Por qué

Es la función central del producto, no un añadido: `mission.md` lo dice con todas
las letras — *"el valor está en recuperar la receta correcta, no en almacenar
muchas"*. Todo lo construido hasta ahora sirve para guardar; esto es lo que sirve
para encontrar.

Va después de publicar (005) porque buscar solo tiene sentido cuando hay recetas
de otros que encontrar.

## Criterios de aceptación

### Buscar

- [x] `GET /recetas/busqueda?nombre=...` encuentra recetas cuyo nombre contenga ese texto.
- [x] `GET /recetas/busqueda?ingrediente=x&ingrediente=y` encuentra las que lleven **todos** esos ingredientes, no cualquiera de ellos.
- [x] `GET /recetas/busqueda?tipo=Postre` filtra por tipo de plato.
- [x] Los tres criterios se pueden combinar y se aplican a la vez.
- [x] Una búsqueda sin ningún criterio devuelve lo que el usuario puede ver, sin filtrar.
- [x] Un tipo de plato fuera de la lista cerrada responde `400`.

### Insensibilidad a mayúsculas y acentos

- [x] Buscar `tortilla` encuentra `Tortilla de patatas`.
- [x] Buscar `JAMON` encuentra una receta llamada `Jamón`.
- [x] Buscar el ingrediente `pimenton` encuentra recetas con `Pimentón`.
- [x] El nombre que se muestra conserva sus acentos: la normalización es solo para buscar.

### Visibilidad

- [x] Encuentra las recetas propias, sean privadas o públicas.
- [x] Encuentra las recetas **públicas de otros usuarios**.
- [x] **No** encuentra las privadas de otros, ni siquiera coincidiendo exactamente con el nombre.
- [x] Cada resultado indica si es del propio usuario.
- [x] Un resultado no expone el correo de su autor.
- [x] Sin token, `401`.

### Límites

- [x] El número de resultados está acotado: una búsqueda muy general no devuelve la base de datos entera. _Implementado (tope de 50), **sin test**: comprobarlo exigiría crear 51 recetas en cada ejecución de la suite._
- [x] La respuesta indica si se han recortado resultados. _Implementado, **sin test**, por el mismo motivo._

### Calidad

- [x] `dotnet build` sin errores ni avisos y `dotnet test` en verde.
- [x] Existe un test que falla si una receta privada ajena aparece en los resultados.
- [x] Existe un test que comprueba que buscar dos ingredientes exige que estén los dos.

## Fuera de alcance

- **Paginación completa.** Hay un tope de resultados y un aviso de recorte; recorrer páginas se añadirá cuando el tope moleste de verdad.
- **Ordenar por relevancia.** Los resultados salen por fecha de modificación. Puntuar coincidencias exige decidir qué pesa más, y sin uso real sería adivinar.
- **Buscar dentro de la elaboración.** Solo nombre, ingredientes y tipo.
- **Búsqueda difusa** (tolerar erratas).
- **Etiquetas libres** — backlog.
- **Interfaz web** — feature 007.
