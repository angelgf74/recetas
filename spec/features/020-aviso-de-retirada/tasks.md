# 020 · Aviso de retirada — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `IEnviadorDeCorreo.EnviarAvisoDeRetiradaAsync`.

## Aplicación

- [x] `RetirarPorModeracion` — comprueba, despublica y avisa.
- [x] `GestionDeRecetas.CambiarVisibilidadAsync` pierde el parámetro `esResponsable` y vuelve a exigir autoría.

## Infraestructura

- [x] Texto del aviso en `MensajesDeCorreo`, con el nombre de la receta escapado.
- [x] Implementación en el enviador de Brevo y en el de consola.

## API

- [x] El `DELETE` de publicación elige entre la gestión normal y la retirada por moderación.

## Pruebas

- [x] Aplicación: retirar avisa **al autor**.
- [x] Aplicación: el autor que despublica lo suyo **no recibe aviso** (integración: `Despublicar_ElAutorNoRecibeAvisoDeRetirada`).
- [x] Aplicación: el responsable no puede retirar una receta **suya** por esta vía.
- [x] Aplicación: no puede retirar una **privada** ajena.
- [x] Aplicación: si el correo falla, **la receta queda retirada igual**.
- [x] Aplicación: retirar dos veces **no manda dos avisos**.
- [x] Integración: el responsable retira; nadie más puede _(ya venía de la 015)_.
- [x] Integración: sigue sin poder editar ni borrar recetas ajenas _(ya venía de la 015)_.
- [x] Integración: el aviso llega al correo de la autora, con el nombre de la receta.
- [x] Integración: el responsable despublicando lo **suyo** no se autoenvía el aviso.

Comprobado que muerden: quitando la llamada a `AvisarAlAutorAsync` fallan tres
(`Retirar_UnaPublicaAjena_LaDespublicaYAvisaAlAutor`, `Retirar_DosVeces_LaSegundaNoAvisa`
y `Retirar_AvisaPorCorreoAlAutor`); con ella, suite completa en verde.

## Cierre

- [x] Suite completa en verde (563 pruebas, 0 fallos).
- [x] Actualizar `tech-stack.md`: la excepción de moderación ya no es un parámetro de `CambiarVisibilidadAsync`.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [ ] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar.

`dotnet format --verify-no-changes` avisa de finales de línea en **todo** el árbol,
incluidos archivos que esta feature no toca: es preexistente y no se corrige aquí.

## Mantenimiento (checklist recurrente)

- [ ] Si algún día el responsable puede hacer algo más sobre contenido ajeno, decidir **para cada acción** si se comunica al afectado. Lo que no se comunica, se descubre.
