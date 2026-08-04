# 014 · Android: paridad con la web — Plan

## Enfoque

La API ya tiene todo lo necesario: es la prueba de que la regla de
`tech-stack.md` —"la API se diseña como si Android ya existiera"— se cumplió. Esta
feature **no toca el servidor**. Si hiciera falta un endpoint nuevo, sería señal
de que el contrato estaba mal y se arreglaría para las dos superficies.

Todo el trabajo es de cliente: ampliar `ClienteDeApi`, añadir acciones al
`AppViewModel` y escribir las pantallas que faltan.

## El único problema de verdad: el alta y la recuperación

Los dos flujos tienen un paso 2 que vive en un enlace enviado por correo, y ese
enlace apunta a la web. Tres opciones:

1. **Que la aplicación no los tenga** y remita a la web. Es lo que hace hoy la
   012, y es justo lo que esta feature viene a quitar.
2. **Pedir al usuario que copie el token** del enlace y lo pegue. Funciona, y es
   horrible.
3. **Enlaces de aplicación (App Links)**: el enlace del correo abre la aplicación
   si está instalada, y si no, la web. Es lo correcto y es lo que se hace.

Los App Links exigen publicar `/.well-known/assetlinks.json` en el dominio con la
huella del certificado de firma. Se publica con la huella de **depuración**, que
es la que existe hoy; cuando haya certificado de publicación habrá que añadir la
suya, y sin eso los enlaces no abrirán la aplicación publicada. Queda anotado
donde se pueda ver.

## Implementación

1. **Contratos**: los tipos de petición que faltan, espejo de `Recetas.Contratos`.
2. **`ClienteDeApi`**: crear, actualizar, borrar, publicar, despublicar, subir y
   borrar fotos, importar, y receta con raciones.
3. **`AppViewModel`**: las acciones y las pantallas nuevas en el estado.
4. **Pantallas**: formulario de receta, acciones de la ficha, escalado, importar,
   alta y recuperación.
5. **Enlaces de aplicación**: filtro de intenciones en el manifiesto,
   `assetlinks.json` en la web y manejo del enlace entrante.

## Decisiones

- **Un solo formulario para crear y editar**, como en la web y por lo mismo:
  duplicarlo garantiza que un arreglo se aplique solo a una de las dos pantallas.
- **La foto se sube leyendo el flujo del `Uri`**, sin copiarla antes a un archivo
  temporal. El tope de 8 MB lo pone el servidor.
- **El escalado se pide al servidor**, igual que en la web. El redondeo a
  cantidades medibles es regla de negocio y no se reimplementa aquí: reimplantarla
  sería exactamente lo que la 010 evitó.
- **La confirmación de borrado es un diálogo**, no un segundo toque. Borrar una
  receta con fotos no se deshace.
- **Las acciones de escritura se ocultan sobre recetas ajenas**, aunque el
  servidor ya las rechace. La autorización real está en el servidor; esto solo
  evita ofrecer lo que va a fallar.

## Riesgos

- **Tamaño.** Es la feature más grande del proyecto en superficie de interfaz.
  Mitigación: se apoya en una API ya cerrada y probada, así que el riesgo es de
  volumen, no de diseño.
- **Los enlaces de aplicación dependen de la firma.** Con el certificado de
  depuración funcionan solo en las compilaciones de depuración. Anotado.
- **Elegir archivos varía entre fabricantes.** Se usa el selector del sistema
  (`GetContent`), que es el camino estándar y no exige permisos de
  almacenamiento.
