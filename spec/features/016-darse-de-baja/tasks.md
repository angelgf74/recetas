# 016 · Darse de baja — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `IRepositorioDeUsuarios.BorrarAsync`.
- [x] `IRepositorioDeSolicitudesDeRegistro.BorrarPorCorreoAsync`.
- [x] `IEnviadorDeCorreo.EnviarConfirmacionDeBajaAsync`.

## Aplicación

- [x] `BorrarCuenta` — verificar contraseña, archivos, recetas, solicitudes, usuario y aviso, en ese orden.
- [x] `ConsultarResumenDeLaCuenta` — cuántas recetas y cuántas fotos, para avisar antes de borrar.

## Infraestructura

- [x] Los tres métodos nuevos en los repositorios y en los dos enviadores.
- [x] Texto del correo de baja en `MensajesDeCorreo`.

## API

- [x] `PeticionDeBaja` en `Recetas.Contratos`.
- [x] `RespuestaDeIdentidad` con el resumen de la cuenta.
- [x] `DELETE /yo`, autenticado.

## Web

- [x] Página `/mi-cuenta`: qué se va a borrar, contraseña y confirmación.
- [x] Al borrarse, cerrar sesión y volver a la portada.
- [x] `wwwroot/borrar-cuenta.html`, estática y legible sin JavaScript.
- [x] `privacidad.html`: quitar el párrafo que dice que no hay botón para borrar la cuenta.

## Android

- [x] `ClienteDeApi.borrarCuenta`.
- [x] Acción en `AppViewModel`, que al terminar cierra sesión.
- [x] Entrada en Ajustes, separada del cierre de sesión.

## Pruebas

- [x] Aplicación: contraseña incorrecta no borra nada.
- [x] Aplicación: el borrado se lleva recetas y **archivos de fotos**, y deja los ingredientes.
- [x] Aplicación: si el correo de confirmación falla, la cuenta queda borrada igual.
- [x] Integración: `401` sin sesión y con contraseña incorrecta, `204` al borrarse.
- [x] Integración: tras la baja, el correo vuelve a estar libre para un alta nueva.
- [x] Integración: sus recetas publicadas desaparecen de la búsqueda de los demás.

## Cierre

- [x] `dotnet format` sin avisos nuevos y suite completa en verde.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar y comprobar la baja contra producción con una cuenta de usar y tirar.
- [ ] Añadir la URL pública en Play Console (*Seguridad de los datos → eliminación de la cuenta*).

## Mantenimiento (checklist recurrente)

- [ ] **Al añadir cualquier tabla colgada del usuario o de la receta, borrarla también aquí.** `recetas.autor_id` no tiene clave foránea, así que la base de datos no avisa: la fila simplemente sobrevive sin dueño.
- [ ] Al añadir un tipo de archivo nuevo en disco, borrarlo en `BorrarCuenta` como se hace con fotos y miniaturas.
