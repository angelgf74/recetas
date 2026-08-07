# 016 · Darse de baja

**Estado:** implementado ✅

## Qué hace

Un usuario puede **borrar su cuenta** desde la aplicación, en web y en Android. Para confirmarlo tiene que escribir su contraseña, y antes de hacerlo se le dice exactamente qué va a desaparecer.

Al borrarse se van **su cuenta, sus recetas, sus fotos y sus denuncias**. También sus recetas publicadas: dejan de estar disponibles para el resto.

Existe además una **página web pública** que explica cómo darse de baja, alcanzable sin tener la aplicación instalada y sin iniciar sesión.

## Por qué

**Bloquea la publicación en Google Play.** Google exige a toda aplicación que permita crear cuentas dos cosas: borrarla desde dentro, y una URL pública donde pedir el borrado sin reinstalar nada. Es el mismo tipo de bloqueo que era la 015.

Hay además una razón que no es Google. La política de privacidad publicada dice hoy:

> "Hoy la aplicación no tiene todavía un botón para eliminar la cuenta entera; se hace escribiendo a la dirección de arriba y se atiende a mano."

Eso era honesto mientras el producto no estaba publicado. Con usuarios de verdad, un borrado que depende de que alguien lea un correo y ejecute SQL a mano no es un procedimiento: es una promesa.

Y el RGPD reconoce el derecho de supresión. Atenderlo a mano funciona con tres usuarios y deja de funcionar con treinta.

## Criterios de aceptación

- [x] Un usuario autenticado puede borrar su cuenta indicando su **contraseña actual**.
- [x] Con la contraseña equivocada responde `401` y **no borra nada**.
- [x] Sin sesión responde `401`.
- [x] Al borrarse desaparecen: el usuario, **sus recetas**, **los archivos de sus fotos y sus miniaturas**, las denuncias que hizo y las que otros hicieron sobre sus recetas, y sus solicitudes de alta y de contraseña pendientes.
- [x] **Los ingredientes no se borran.** Son catálogo compartido: borrarlos rompería las recetas de otros.
- [x] Sus recetas **publicadas** dejan de aparecer en la búsqueda de los demás.
- [x] Tras borrarse, **el correo vuelve a estar libre**: se puede crear una cuenta nueva con esa dirección.
- [x] Antes de confirmar, la aplicación **dice cuántas recetas y cuántas fotos** se van a borrar, y que la acción no se puede deshacer. — El dato viaja en `GET /yo` y hay test de que es correcto. **La pantalla que lo enseña no se ha visto**, ni en el navegador ni en el teléfono.
- [x] Se envía un **correo de confirmación** a la dirección borrada. Si el envío falla, la cuenta **igualmente queda borrada**.
- [x] Existe una página pública en `/borrar-cuenta.html`, **legible sin JavaScript y sin sesión**, que explica el procedimiento y a quién escribir. — **Pendiente de comprobar servida** en producción, como se hizo con `privacidad.html`.
- [x] La página de privacidad deja de decir que no hay forma de borrar la cuenta.

## Fuera de alcance

- **Periodo de gracia o papelera.** Borrar borra. Un "te quedan 30 días" obliga a mantener las cuentas a medio existir, decidir si su correo está libre y qué pasa con sus recetas públicas mientras tanto. Con este tamaño de producto no compensa.
- **Exportar los datos antes de borrar.** Es el derecho de portabilidad y merece su propia feature; hoy se atiende escribiendo, como dice la política.
- **Borrar solo el contenido y conservar la cuenta.** Ya se puede: las recetas se borran una a una.
- **Que el responsable borre cuentas ajenas.** La moderación de la 015 llega hasta retirar contenido. Suspender a alguien es una decisión que hoy se toma a mano y no necesita interfaz.
- **Invalidar el JWT en el momento del borrado.** Los tokens son sin estado desde la 002: el de una cuenta borrada sigue estando firmado hasta que caduca, pero no sirve para nada porque no queda nada suyo que leer. Cerrar sesiones sigue en el backlog.
