# 016 · Darse de baja — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

`DELETE /yo`, con la contraseña en el cuerpo. El recurso ya existe —`GET /yo` devuelve la identidad— y borrarse es borrar ese recurso, no invocar un verbo.

**El borrado lo orquesta la aplicación, no la base de datos.** Aquí no vale confiarse a las cascadas: `recetas.autor_id` **no tiene clave foránea** a `usuarios`, solo un índice, así que borrar la fila del usuario dejaría sus recetas vivas y sin dueño. Y aunque la tuviera, la cascada nunca tocaría los archivos del disco.

El orden es el mismo que ya usa el borrado de una receta, por el mismo motivo: **archivos primero, filas después**. Al revés quedarían fotos que ninguna fila menciona, invisibles y ocupando espacio para siempre.

## Implementación

1. **Dominio — `IRepositorioDeUsuarios`.** Un método `BorrarAsync`.

2. **Dominio — `IRepositorioDeSolicitudesDeRegistro`.** `BorrarPorCorreoAsync`: esas solicitudes se identifican por correo y **no tienen clave foránea** al usuario, así que nadie las arrastra. Dejarlas permitiría completar un alta pendiente con un enlace anterior a la baja.

3. **Dominio — `IEnviadorDeCorreo`.** `EnviarConfirmacionDeBajaAsync`.

4. **Aplicación — `Recetas.Aplicacion/Cuentas/BorrarCuenta.cs`.** En este orden: buscar al usuario, **verificar la contraseña**, listar sus recetas, borrar los archivos de cada foto, borrar las recetas, borrar sus solicitudes de alta, borrar al usuario y por último avisar por correo.

5. **Aplicación — `ResumenDeLaCuenta`.** Cuántas recetas y cuántas fotos tiene, para que la pantalla pueda decir qué se va a perder antes de confirmar.

6. **Infraestructura.** Implementar los métodos nuevos y añadir el texto del correo a `MensajesDeCorreo`.

7. **Contratos.** `PeticionDeBaja` con la contraseña. `RespuestaDeIdentidad` gana el resumen.

8. **API.** `DELETE /yo` autenticado, y `GET /yo` devolviendo también el resumen.

9. **Web.** Página `/mi-cuenta` con lo que se va a borrar, el campo de contraseña y confirmación explícita. Al terminar, cierra sesión y vuelve a la portada.

10. **Web — `wwwroot/borrar-cuenta.html`.** Página pública, estática, sin JavaScript, como `privacidad.html`.

11. **Web — `privacidad.html`.** Cambiar el párrafo que dice que no hay botón para borrar la cuenta.

12. **Android.** Entrada en Ajustes con el mismo diálogo.

13. **Tests.** Aplicación: contraseña incorrecta no borra nada, el borrado se lleva recetas y archivos, los ingredientes sobreviven, el fallo del correo no aborta. Integración: los códigos de estado, que el correo queda libre y que las recetas públicas desaparecen para los demás.

## Decisiones

- **Se exige la contraseña, no basta la sesión** — es la acción más destructiva del producto y no se puede deshacer. Un teléfono desbloqueado un momento no debe bastar. Es además lo que separa "cerrar sesión" de "borrar la cuenta" en la cabeza del usuario.

- **`DELETE /yo` y no `DELETE /usuarios/{id}`** — nadie borra a otro, así que un identificador en la ruta solo abriría la duda de qué pasa si no es el tuyo. El sujeto sale del token.

- **La contraseña viaja en el cuerpo de un DELETE** — poco habitual pero correcto: en la URL acabaría en los registros del servidor y en el historial del navegador. HTTP no lo prohíbe y tanto `HttpClient` como Ktor lo permiten.

- **Sin periodo de gracia** — una cuenta a medio borrar obliga a decidir si su correo está libre, si sus recetas públicas se siguen viendo y qué pasa si vuelve. Son tres preguntas nuevas para resolver un caso —"me he arrepentido"— que aquí se arregla volviéndose a registrar.

- **El correo se libera** — no se guarda ninguna marca de "esta dirección tuvo cuenta". Guardarla sería conservar un dato personal de quien pidió justo lo contrario.

- **Los ingredientes no se tocan** — son catálogo compartido entre todos los usuarios. Borrar los que usaba esta cuenta rompería recetas ajenas.

- **El aviso por correo va al final y su fallo no se propaga** — mismo criterio que en las denuncias: la cuenta ya no existe, y devolver un error haría creer que no se borró.

- **La página pública es estática** — la revisa alguien de Google que puede llegar sin sesión y con JavaScript desactivado. Blazor WebAssembly no serviría nada legible en ese caso, igual que pasó con `privacidad.html`.

## Riesgos

- **Borrado a medias** — no hay transacción que abarque disco y base de datos. Si falla entre medias, quedan archivos borrados y filas vivas: la operación se puede repetir y termina el trabajo. El orden elegido hace que el resto sea consistente; al revés dejaría basura irrecuperable.

- **Confundir cerrar sesión con borrar la cuenta** — van en la misma pantalla de Ajustes. Se separan visualmente, el botón de borrar exige contraseña y la confirmación dice en palabras qué desaparece.

- **Recetas huérfanas si alguien añade una tabla nueva** — cualquier entidad futura colgada del usuario hay que acordarse de borrarla aquí, porque `autor_id` no tiene clave foránea que ayude. Queda anotado en el mantenimiento de `tasks.md`.

- **El token sigue firmado tras la baja** — hasta siete días. No sirve para nada: no queda ninguna fila suya que leer. Es la misma consecuencia de los JWT sin estado que ya arrastra la 008.
