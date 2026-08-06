# 015 · Denunciar contenido — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

La denuncia es un **recurso propio colgando de la receta** (`POST /recetas/{id}/denuncias`), igual que las fotos. No es un verbo suelto ni un campo de la receta: es un hecho con autor, motivo y fecha, que se guarda y se consulta.

Se guarda en base de datos **además** de enviarse por correo. El correo es el aviso; la fila es la prueba de que existió el proceso. Si el aviso se pierde —Brevo caído, buzón lleno— la denuncia sigue estando.

La retirada **no estrena un concepto de administrador en el dominio**. `Receta` sigue sabiendo únicamente `EsDe` y `PuedeVerla`; quién puede moderar es una decisión de la capa de aplicación, resuelta comparando el correo del JWT con uno de configuración. Meter roles en el dominio por un único moderador sería pagar un modelo de permisos entero para una lista de un elemento.

## Implementación

1. **Dominio — `Recetas.Dominio/Moderacion/MotivoDeDenuncia.cs`.** Enumerado cerrado: `NoEsUnaReceta`, `Ofensivo`, `Sexual`, `Violento`, `Spam`, `Derechos`, `Otro`.

2. **Dominio — `Recetas.Dominio/Moderacion/Denuncia.cs`.** Entidad con `Id`, `RecetaId`, `DenuncianteId`, `Motivo`, `Comentario` (opcional, acotado) y `FechaDeCreacion`. Fábrica `Crear` que valida la longitud del comentario.

3. **Dominio — `Recetas.Dominio/Puertos/IRepositorioDeDenuncias.cs`.** `AnadirAsync`, `YaDenuncioAsync(denuncianteId, recetaId)` y `GuardarCambiosAsync`.

4. **Dominio — `Recetas.Dominio/Puertos/IEnviadorDeCorreo.cs`.** Un método más: `EnviarAvisoDeDenunciaAsync`, con el destinatario, la receta y los datos de la denuncia.

5. **Aplicación — `Recetas.Aplicacion/Moderacion/GestionDeDenuncias.cs`.** Comprueba en este orden: la receta existe y **el denunciante puede verla** (si no, `NoEncontrada`), **no es suya** (si lo es, `EsPropia`), y **no la ha denunciado ya** (si sí, sale como correcta sin escribir). Después guarda y envía el aviso.

6. **Aplicación — `GestionDeRecetas.DespublicarAsync`.** Acepta un indicador de que quien pide la retirada es el responsable. Con él, la comprobación deja de ser `EsDe` y pasa a permitir cualquier receta pública. Sin él, todo sigue igual.

7. **Infraestructura — `OpcionesDeModeracion`** con `CorreoDelResponsable`, más `ConfiguracionDeDenuncia` (mapeo EF, tabla `denuncias`, **índice único por denunciante y receta**) y `RepositorioDeDenunciasEf`.

8. **Infraestructura — `EnviadorDeCorreoBrevo` y el de desarrollo.** Implementan el método nuevo.

9. **Migración** `Denuncias`.

10. **Contratos — `Recetas.Contratos/Moderacion/ContratosDeDenuncia.cs`.** `PeticionDeDenuncia` con `Motivo` (texto, no número) y `Comentario`.

11. **API — `Recetas.Api/Endpoints/DenunciasEndpoints.cs`.** `POST /recetas/{id}/denuncias`, autenticado y con límite de frecuencia propio.

12. **API — `RecetasEndpoints`.** El `DELETE /recetas/{id}/publicacion` mira si el correo del JWT es el del responsable y lo pasa a la capa de aplicación.

13. **Web — `Recetas.Web`.** En la ficha de una receta ajena, botón «Denunciar» que abre un formulario con los motivos y un texto opcional.

14. **Android — ficha.** El mismo botón y un diálogo equivalente, más el método en `ClienteDeApi` y la acción en `AppViewModel`.

15. **Tests.** Los de aplicación cubren el orden de comprobaciones; los de integración, los códigos de estado y la idempotencia.

## Decisiones

- **La denuncia se guarda, no solo se envía por correo** — un correo perdido no puede significar que la denuncia no existió. Además permite responder «ya la denunciaste» sin volver a molestar al responsable.

- **Segunda denuncia del mismo usuario: `201` sin efecto** — responder un error obligaría al cliente a distinguir dos casos que al usuario le dan igual, y decirle «ya la denunciaste» filtra menos que un error. El índice único es la red de seguridad si dos peticiones llegan a la vez.

- **Denunciar una receta privada ajena responde `404`, no `403`** — la misma regla que rige toda la API: un `403` confirmaría que la receta existe. Se reutiliza `PuedeVerla`.

- **Denunciar la receta propia es `400`, no `404`** — el usuario la ve, así que ocultarla sería absurdo. Es un error de uso, no un problema de permisos.

- **El moderador se identifica por correo en configuración, no por un campo en la base** — hay exactamente un responsable y no se prevé un segundo. Un `EsAdministrador` en `usuarios` invitaría a construir gestión de roles alrededor. Si algún día hay más de uno, la configuración pasa a lista y el resto no cambia.

- **El correo del responsable sale del JWT, no de la base** — el token ya lleva el `email` del usuario, así que la comprobación no cuesta una consulta. El token lo firma el servidor: su contenido no es manipulable.

- **Retirar es despublicar, no borrar** — el autor no pierde su trabajo por una denuncia que puede ser infundada, y deshacerlo es publicar de nuevo. Borrar sería irreversible y desproporcionado como primera medida.

- **Se reutiliza `Receta.Despublicar()`** — la operación de dominio es la misma; lo que cambia es quién tiene derecho a pedirla, y eso es autorización, que vive en aplicación.

## Riesgos

- **El endpoint como buzón de spam** — límite de frecuencia propio, y el índice único impide repetir sobre la misma receta. Un usuario decidido puede denunciar muchas recetas distintas, pero eso ya requiere una cuenta verificada por correo.

- **Confundir moderación con propiedad** — es la trampa que `CLAUDE.md` señala entre `EsDe` y `PuedeVerla`. El indicador de responsable se pasa explícito y solo afecta a despublicar: no abre editar, ni borrar, ni ver privadas ajenas. Hay test de que un responsable **no** puede editar una receta ajena.

- **Un fallo del correo tumbando la denuncia** — el envío va después de guardar y su error se registra sin propagarse. El usuario recibe confirmación porque su denuncia sí quedó anotada.

- **La ficha de Android y la de la web divergiendo** — el criterio para mostrar el botón es el mismo campo que ya distingue receta propia de ajena (`EsMia`), no una regla nueva en cada cliente.
