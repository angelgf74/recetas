# 015 · Denunciar contenido

**Estado:** implementado ✅

## Qué hace

Cualquier usuario que se encuentre una **receta pública ajena** con contenido inapropiado puede denunciarla desde la propia aplicación: elige un motivo de una lista corta, puede añadir una explicación, y envía.

La denuncia queda guardada y **avisa por correo al responsable del servicio**, que puede **retirar la receta de la parte pública** sin necesidad de entrar en la base de datos.

Retirar una receta la devuelve a privada: **no se borra nada**. El autor la conserva y sigue viéndola en su recetario.

## Por qué

Recetas permite publicar: un usuario sube texto y fotos, y cualquier otro usuario registrado los ve al buscar. Eso es contenido generado por usuarios, y la política de Google Play para aplicaciones que lo permiten exige **una forma de denunciar dentro de la aplicación** y **que la denuncia tenga consecuencia**.

Hoy no hay ni lo uno ni lo otro. En el cuestionario de clasificación de contenido hay que responder que sí a *«uso compartido de contenido del usuario»* —responder que no sería falso y se comprueba en un minuto abriendo la búsqueda—, y esa respuesta activa el requisito. **Sin esto, la aplicación no se puede publicar.**

Hay además una razón que no es Google: hoy, si alguien publica algo que no debe, la única forma de quitarlo es un `UPDATE` a mano en producción. Eso no es un procedimiento de moderación, es un parche.

## Criterios de aceptación

- [x] Un usuario autenticado puede denunciar una receta **pública que no es suya**, indicando un motivo de la lista cerrada y, opcionalmente, un texto libre.
- [x] Denunciar la **receta propia** responde `400`: no tiene sentido y evita ruido.
- [x] Denunciar una receta **privada ajena** responde `404`, igual que pedirla — no se puede usar el endpoint para averiguar si existe.
- [x] Denunciar sin sesión responde `401`.
- [x] La denuncia queda **guardada** con quién, qué receta, motivo, texto y fecha.
- [x] El mismo usuario no puede denunciar dos veces la misma receta: la segunda responde igual que la primera, pero **no crea una fila nueva ni manda otro correo**.
- [x] Al recibirse una denuncia se **envía un correo** al responsable con el motivo, el texto, y el nombre e identificador de la receta.
- [x] Si el envío del correo falla, la denuncia **igualmente queda guardada** y el usuario ve que se ha enviado. El aviso es un extra, no la garantía.
- [x] El responsable —una cuenta identificada por su correo en la configuración— puede **retirar de la parte pública** cualquier receta, aunque no sea suya.
- [x] Retirar una receta la deja **privada, no borrada**: su autor la sigue viendo y puede volver a publicarla.
- [x] Un usuario que no es el responsable **no puede** retirar recetas ajenas: responde `404`.
- [x] La ficha de una receta ajena publicada ofrece **denunciar**, en web y en Android. La ficha de una receta propia, no. — **Escrito y compilado, no probado en el navegador ni en el teléfono.**
- [x] Tras denunciar, la aplicación **confirma** y no vuelve a ofrecer denunciar esa receta en la misma sesión. — **Igual: sin comprobación visual.**
- [x] El endpoint está **limitado por frecuencia**: no se puede usar para inundar el buzón del responsable. — Diez por hora y origen. **No hay test del `429`**, a diferencia del que sí existe para el alta.

### Lo que la implementación añadió sobre lo previsto

- El responsable **tampoco puede publicar una receta privada ajena**. La spec solo decía que puede retirar; al implementarlo quedó claro que un `esResponsable` sin restringir habría permitido lo contrario, que es exponer contenido que su autor no compartió. Hay test.
- El nombre de la receta y el comentario **van escapados en el correo**: los escribe un usuario y acaban en un mensaje que el responsable abre con confianza.

## Fuera de alcance

- **Panel de moderación.** El correo y la retirada bastan para el volumen de este producto. Una bandeja con estados y filtros no se justifica hasta que haya denuncias de verdad.
- **Bloquear usuarios.** Recetas no tiene perfiles, mensajes ni forma de dirigirse a otro usuario: una receta pública muestra su contenido, no quién la escribió. Sin identidad visible ni interacción, no hay a quién bloquear. Si algún día hay comentarios, esto vuelve.
- **Suspender o borrar cuentas.** Es una decisión que hoy se toma a mano y no necesita interfaz.
- **Denunciar otras cosas** (fotos sueltas, ingredientes). La unidad que se publica es la receta, y con ella va todo lo demás.
- **Avisar al autor** de que su receta ha sido retirada. Necesita decidir qué se le cuenta y cómo se recurre; se anota en el backlog.
