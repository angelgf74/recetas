# 021 · Favoritos privados — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `Favorito` — `UsuarioId`, `RecetaId`, `FechaDeMarca`, sin identificador propio.
- [x] `IRepositorioDeFavoritos` — `EstaMarcadaAsync`, `AnadirAsync`, `QuitarAsync`.
- [x] `IRepositorioDeRecetas.ListarFavoritasAsync` — con el filtro de visibilidad **dentro** de la consulta.

## Aplicación

- [x] `GestionDeFavoritos` — marcar (exige `PuedeVerla`), desmarcar (no exige nada) y listar.
- [x] Registrar el caso de uso en la inyección de dependencias.

## Infraestructura

- [x] `ConfiguracionDeFavorito` — tabla `favoritos`, clave compuesta, cascadas a `recetas` y `usuarios`, índice por `receta_id`.
- [x] `DbSet<Favorito>` en el contexto.
- [x] `RepositorioDeFavoritosEf` y `ListarFavoritasAsync` en `RepositorioDeRecetasEf`.
- [x] Migración `20260809100120_Favoritos`.

## API

- [x] `PUT /recetas/{id}/favorito` y `DELETE /recetas/{id}/favorito`, ambos idempotentes.
- [x] `GET /recetas/favoritas`.
- [x] `RespuestaDeReceta.EsFavorita`.

## Web

- [x] `ClienteDeApi`: marcar, desmarcar y listar.
- [x] Botón en la ficha, **con texto** y no solo un corazón.
- [x] Página `/favoritos` con las tarjetas y miniaturas del recetario.
- [x] Enlace en la navegación.

## Pruebas

- [x] Aplicación: marcar una pública ajena y aparece en mi lista.
- [x] Aplicación: marcar la propia también vale.
- [x] Aplicación: marcar una **privada ajena** no se puede.
- [x] Aplicación: marcar dos veces no duplica.
- [x] Aplicación: desmarcar lo no marcado no es error.
- [x] Aplicación: **despublicada, desaparece de la lista; republicada, vuelve**.
- [x] Aplicación: desmarcar sigue siendo posible sobre algo que ya no se ve.
- [x] Aplicación: marcar **no toca la fecha de modificación** de la receta.
- [x] Integración: `404` al marcar una privada ajena.
- [x] Integración: mis favoritos no salen en la respuesta que ve **otro** usuario.
- [x] Integración: la ficha no lleva ningún recuento.
- [x] Integración: borrar la receta se lleva la marca.
- [x] Integración: darse de baja se lleva los favoritos.
- [x] bUnit: la ficha ofrece marcar o desmarcar según el estado, y también sobre lo propio.

Comprobado que muerden:

- Quitando el filtro de visibilidad de la consulta de EF falla
  `Favoritas_NoDevuelveLoQueDejoDeEstarPublicado`.
- Quitando el texto del botón de la ficha fallan los tres de bUnit.

## Cierre

- [x] Suite completa en verde (588 pruebas, 0 fallos).
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Anotar en el backlog la decisión pendiente sobre exportar los favoritos (019).
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar.

## Mantenimiento (checklist recurrente)

- [ ] Si algún día se añade un agregado sobre favoritos —recuento, "lo más marcado", recomendaciones—, releer `mission.md` antes: es lo mismo que las valoraciones que descarta, con otro nombre.
