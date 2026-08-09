# 020 · Aviso de retirada — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

**La retirada por moderación sale de `GestionDeRecetas` y pasa a ser un caso de uso propio**, `RetirarPorModeracion`, junto a `GestionDeDenuncias`.

Hoy vive dentro de `CambiarVisibilidadAsync` como un parámetro `esResponsable`, y eso funcionaba mientras la operación era idéntica. Deja de serlo en cuanto una avisa por correo y la otra no: seguir mezclándolas obligaría a meter el repositorio de usuarios, el enviador de correo y un registro dentro de la gestión del recetario propio, solo para un camino que no es suyo.

Separarlas tiene además una ventaja de seguridad: **`GestionDeRecetas` vuelve a saber solo de autoría**, y desaparece el parámetro que podía colarse en otra operación por descuido.

El endpoint es el mismo (`DELETE /recetas/{id}/publicacion`) y sigue sirviendo a los dos actores; lo que cambia es a quién le pasa la petición.

## Implementación

1. **Dominio — `IEnviadorDeCorreo`.** Un método más: `EnviarAvisoDeRetiradaAsync`, con el destinatario y el nombre de la receta.

2. **Aplicación — `Recetas.Aplicacion/Moderacion/RetirarPorModeracion.cs`.** Comprueba que la receta existe, que **no es del propio responsable** y que **está publicada**; la despublica, y después avisa al autor. El fallo del correo se registra y no se propaga.

3. **Aplicación — `GestionDeRecetas.CambiarVisibilidadAsync`.** Pierde el parámetro `esResponsable` y vuelve a exigir autoría, como antes de la 015.

4. **Infraestructura — `MensajesDeCorreo`** y los dos enviadores. El nombre de la receta va escapado, igual que en el aviso de denuncia.

5. **API — `RecetasEndpoints`.** En el `DELETE`, si quien pide es el responsable y la receta no es suya, va a `RetirarPorModeracion`; si no, a la gestión normal.

6. **Tests.** Aplicación: que avisa al autor y no a otros, que no avisa cuando despublica el propio autor, y que un fallo del correo no deshace la retirada. Integración: que el responsable sigue pudiendo retirar y que nadie más puede.

## Decisiones

- **Sin el motivo de la denuncia.** La retirada la decide el responsable, no la denuncia: puede retirar sin que nadie haya denunciado, y puede haber varias con motivos distintos. Dar uno como razón oficial sería inventar una precisión que no existe, y además acercaría el aviso a filtrar lo que escribió un tercero.

- **Sin quién denunció, en ninguna forma.** Ni el correo, ni cuántos, ni el comentario. En una comunidad pequeña, "te han denunciado dos personas" es casi decir quiénes.

- **Caso de uso propio en lugar de una bandera** — ver el enfoque. Es la misma razón por la que `Receta` distingue `EsDe` de `PuedeVerla`: cuando dos cosas dejan de significar lo mismo, compartir el camino es lo que produce los errores.

- **El aviso va después de despublicar, y su fallo no la deshace** — mismo criterio que en las denuncias y en la baja. La receta ya está retirada; devolver un error haría pensar al responsable que no ha surtido efecto y volvería a intentarlo.

- **El texto no pide disculpas ni acusa.** Dice qué ha pasado, que no se ha perdido nada y a quién escribir. Un aviso de moderación que suena a sanción invita a discutir; uno que suena a error del sistema invita a ignorarlo.

## Riesgos

- **Que el aviso llegue al buzón de correo no deseado**, y el autor se entere igualmente mirando. No hay mucho que hacer: el dominio ya está autenticado con SPF y DKIM, que es lo que está en nuestra mano.

- **Retirada por error.** El aviso lo hace visible en lugar de silencioso, que es justo lo que se busca: si el responsable se equivoca, el autor puede decirlo. Sin aviso, el error queda enterrado.

- **Perder la simetría con la baja de cuenta.** Al borrarse una cuenta, sus recetas públicas desaparecen sin avisar a nadie — pero ahí el afectado es quien pulsa, así que no hay a quién avisar.
