# 015 · Denunciar contenido — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `MotivoDeDenuncia` — enumerado cerrado de motivos.
- [x] `Denuncia` — entidad con fábrica que valida el comentario.
- [x] `IRepositorioDeDenuncias` — añadir, comprobar si ya existe, guardar.
- [x] `IEnviadorDeCorreo` — método de aviso de denuncia.
- [x] `Receta.EsPublica` — estado, no permiso: lo consulta la moderación.

## Aplicación

- [x] `GestionDeDenuncias` — orden de comprobaciones: puede verla, no es suya, no repetida.
- [x] `GestionDeRecetas.CambiarVisibilidadAsync` — admite la retirada por el responsable.
- [x] `CorreoDelResponsable` — quién modera, como configuración y no como rol.

## Infraestructura

- [x] `ConfiguracionDeDenuncia` — tabla `denuncias` con índice único por denunciante y receta.
- [x] `RepositorioDeDenunciasEf`.
- [x] Aviso de denuncia en el enviador de Brevo y en el de consola, con el texto del usuario escapado.
- [x] Migración `Denuncias`.

## API

- [x] `PeticionDeDenuncia` en `Recetas.Contratos`.
- [x] `DenunciasEndpoints` — `POST /recetas/{id}/denuncias`, autenticado.
- [x] Límite de frecuencia propio para denuncias.
- [x] `DELETE /recetas/{id}/publicacion` — reconoce al responsable por el correo del JWT.
- [x] Registro de `CorreoDelResponsable` en `Program`, no en Infraestructura: el tipo vive en Aplicación.

## Web

- [x] Botón «Denunciar» en la ficha de una receta ajena, y solo ahí.
- [x] Formulario con los motivos y el comentario opcional, y confirmación al enviar.

## Android

- [x] `ClienteDeApi.denunciar`.
- [x] `AppViewModel.denunciar`, con las ya denunciadas en el estado.
- [x] Botón y diálogo en la ficha de una receta ajena.

## Pruebas

- [x] Aplicación: receta propia, receta privada ajena, denuncia repetida, fallo del correo, sin responsable configurado.
- [x] Aplicación: el responsable **no** puede editar ni borrar recetas ajenas, ni publicar una privada ajena.
- [x] Integración: `401` sin sesión, `404` en privada ajena, `400` en propia y en motivo inventado, `204` en ajena pública.
- [x] Integración: retirada por el responsable, rechazo a quien no lo es, y que el responsable no ve privadas ajenas.

## Cierre

- [x] `dotnet format` sin avisos nuevos y suite completa en verde (510 pruebas).
- [x] Documentar `Moderacion:CorreoDelResponsable` en `appsettings.Example.json` y en `deploy/README.md`.
- [x] Actualizar `CLAUDE.md`: la moderación es una tercera pregunta distinta de `EsDe` y `PuedeVerla`.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar y comprobar la denuncia contra producción.
- [ ] Responder «Sí» en el cuestionario de contenido de Play.

## Mantenimiento (checklist recurrente)

- [ ] Si cambia el correo del responsable, cambiarlo en `/etc/recetas/api.env` **y** comprobar que existe una cuenta con ese correo: si no coincide con ninguna, las denuncias llegan pero nadie puede retirar nada.
