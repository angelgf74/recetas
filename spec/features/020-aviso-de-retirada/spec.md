# 020 · Aviso de retirada

**Estado:** hecha

## Qué hace

Cuando el responsable del servicio **retira una receta de la parte pública**, su autor recibe un correo: qué receta ha sido, que **sigue siendo suya y no se ha borrado nada**, y a quién escribir si cree que hay un error.

El aviso solo se envía cuando retira el responsable. Si el autor deja de compartir su propia receta, no recibe nada: ya sabe lo que ha hecho.

## Por qué

**Hoy el autor se entera mirando.** La 015 le dio al responsable la capacidad de retirar contenido ajeno, y eso está bien; lo que no está bien es que la persona afectada tenga que descubrirlo por su cuenta, sin saber qué ha pasado ni a quién preguntar.

Es lo que separa moderar de censurar: **una decisión sobre el contenido de alguien se le comunica a esa persona**, y se le da una vía para responder. La 015 dejó esto fuera a propósito, anotando que había que decidir qué se le cuenta.

## Criterios de aceptación

- [x] Al retirar el responsable una receta pública ajena, se envía un correo **al autor**.
- [x] El correo dice **qué receta** y que **no se ha borrado**: sigue en su recetario, ahora privada.
- [x] El correo ofrece **a quién escribir** si cree que es un error — responder al mensaje, que sale con `replyTo` real.
- [x] **No revela quién denunció**, ni cuántas denuncias hubo, ni el comentario que escribieron: el caso de uso solo recibe la receta, no la denuncia.
- [x] **No se envía** cuando el autor despublica su propia receta.
- [x] **No se envía** cuando el responsable despublica una receta suya: ahí actúa como autor.
- [x] Si el envío falla, **la retirada se mantiene**: el aviso es una cortesía, no una condición.
- [x] El nombre de la receta va **escapado** en el correo: lo escribe un usuario.

**No se ha visto un correo real.** Todo lo anterior está comprobado con el
enviador espía; el texto que sale por Brevo no se ha mirado en una bandeja de
entrada.

## Fuera de alcance

- **Explicar el motivo concreto.** La retirada la decide el responsable, no la denuncia: puede retirar sin que nadie haya denunciado, y puede haber varias denuncias con motivos distintos. Dar uno como si fuera "la razón oficial" sería inventar precisión que no existe. Quien quiera saber más, escribe.
- **Un procedimiento de recurso.** El correo de contacto basta para el tamaño de este producto. Un flujo de apelaciones dentro de la aplicación es otra cosa.
- **Avisar al denunciante** de que su denuncia prosperó. Sabría qué recetas están siendo retiradas y por quién, que no es asunto suyo.
- **Avisar de otras acciones.** Hoy retirar es la única que el responsable puede hacer sobre contenido ajeno. Si algún día hay más, cada una decidirá si se comunica.
