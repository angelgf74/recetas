# 027 · Importar también la foto de la receta

**Estado:** hecha

## Qué hace

Al importar una receta desde una URL (011), si la página declara una foto en
su `schema.org/Recipe`, el borrador la trae ya descargada y lista para
guardarse en cuanto el usuario envíe el formulario.

## Por qué

La 011 solo traía texto a propósito: "descargar y republicar la imagen de un
tercero tiene más aristas que el texto" quedó anotado en su momento. Sigue
siendo cierto —es la razón de que esta feature tenga su propia spec en vez de
ser una ampliación de tres líneas—, pero dejar la foto fuera para siempre deja
la receta importada visiblemente incompleta frente a lo que el usuario veía en
la página de origen.

## Por qué no es una ampliación trivial

Esas aristas, una por una:

- **Es una segunda dirección que escribe un tercero**, no el usuario: la URL de
  la imagen la pone quien escribió la página, y el servidor la va a pedir
  igual que pidió la página. Es la misma superficie de SSRF que la 011 ya
  contuvo, aplicada a un destino distinto.
- **`POST /recetas/importaciones` no crea nada** (011): no hay receta a la que
  colgar una foto todavía, así que no vale reutilizar
  `POST /recetas/{id}/fotos` sin más.
- **Una foto de un tercero necesita la misma limpieza que una del usuario**: el
  EXIF puede llevar ubicación GPS de quien la hizo, no de quien importa.

## Cómo se resuelve

- La descarga de la imagen pasa por el **mismo cliente HTTP endurecido** que ya
  usa la 011 para la página: mismo `ConnectCallback` que valida la IP al
  conectar, mismas redirecciones controladas a mano, mismo tope de tiempo y de
  bytes. No hay una ruta nueva hacia la red; es la misma con un destino
  distinto.
- El servidor descarga y **limpia** la imagen —mismo `ILimpiadorDeImagenes` y
  la misma detección de formato por bytes que usan las fotos subidas por el
  usuario (009)— **durante la propia llamada de importar**, y devuelve los
  bytes ya limpios en el borrador. El cliente los guarda en memoria y, si el
  usuario envía el formulario, los sube con `POST /recetas/{id}/fotos` justo
  después de crear la receta. El servidor no guarda nada hasta que la receta
  existe: el borrador sigue sin crear nada por sí mismo.

## Criterios de aceptación

- [x] Una página con `image` en su `Recipe` de `schema.org` hace que el borrador incluya la imagen, ya limpia de metadatos.
- [x] Al guardar la receta desde ese borrador, la foto queda asociada a la receta recién creada.
- [x] Una página sin `image`, o con un valor que no es una URL http(s) válida, no rompe la importación: el borrador llega igual, solo que sin foto.
- [x] Si la imagen no se puede descargar, no es un formato admitido, o supera el tamaño máximo, la importación sigue siendo correcta y el borrador llega sin foto.
- [x] La dirección de la imagen está sujeta a las mismas reglas de la 011: si apunta a la red interna del servidor, no se llega a pedir, y el fallo es indistinguible de "no se ha podido descargar".
- [x] La imagen importada pasa por la misma limpieza de metadatos que una foto subida directamente.

## Nota sobre cómo se comprobó la protección de red

El test más importante de esta feature no fue el que salió bien a la primera.
Los primeros intentos de probar que `DescargarImagenAsync` no alcanza la red
interna apuntaban a `127.0.0.1`, `10.0.0.1`, `169.254.169.254`… y pasaban. Pero
pasaban **también con un cliente sin ninguna protección**, comprobado a
propósito saboteando el código: en esta máquina de desarrollo no hay nada
escuchando en esas direcciones, así que "no se pudo conectar por estar
bloqueado" y "no se pudo conectar porque no había nadie" daban el mismo
resultado observable. El test no distinguía nada.

El test que sí distingue levanta un servidor de verdad en `127.0.0.1`, listo
para responder con una imagen válida, y comprueba que el descargador **nunca
llega a hablar con él**. Con un cliente sin protección, ese test falla de
verdad —comprobado— y con el real, pasa. Queda como
`DescargarImagen_NuncaHablaConUnServidorEnBucleLocal_AunqueEsteListoParaResponder`
en `Recetas.Infraestructura.Tests`. Los tests contra direcciones fijas se
quedan igualmente, documentados con esta misma limitación en su comentario:
no está de más tenerlos por si algún día corren en un sitio donde sí hay algo
escuchando ahí.

## Fuera de alcance

- **Varias fotos.** Solo la primera imagen del `Recipe`, igual que la propia
  receta importa "de una en una" (011).
- **Reintentar la descarga de la imagen sola** si falla, sin volver a importar
  la página entera.
- **Imágenes en `data:` URI** incrustadas en el JSON-LD. Solo direcciones
  http(s); una `data:` URI se ignora igual que cualquier otro valor que no
  encaje.
