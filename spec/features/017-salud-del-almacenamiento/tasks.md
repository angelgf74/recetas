# 017 · Salud del almacenamiento — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Dominio

- [x] `IComprobadorDeAlmacenDeFotos` — puerto nuevo, hermano del de la base de datos.
- [x] `EstadoDeSalud` pasa de enumerado a registro con `BaseDeDatos`, `Almacenamiento` y `EsCorrecto` derivado.

## Aplicación

- [x] `ConsultarSalud` pregunta a los dos puertos **en paralelo** y compone el resultado.
- [x] Una excepción de cualquiera de los dos se traduce a degradado, nunca se propaga.

## Infraestructura

- [x] `ComprobadorDeAlmacenDeFotosEnDisco` — existe, se puede escribir, queda espacio.
- [x] El archivo de prueba se borra siempre, también si algo falla por el camino.
- [x] `OpcionesDeFotos.MinimoDeEspacioLibreEnMb`, con valor por defecto.

## API

- [x] `RespuestaDeSalud` gana el campo `Almacenamiento`.
- [x] El endpoint compone la respuesta y decide `200` o `503` en un único punto.

## Pruebas

- [x] Dominio: `EsCorrecto` solo con las dos piezas sanas.
- [x] Aplicación: las cuatro combinaciones, y la excepción traducida a degradado.
- [x] Infraestructura: directorio inexistente, sin permiso de escritura, y umbral por encima del espacio real.
- [x] Infraestructura: la comprobación **no deja archivos** en el directorio.
- [x] Integración: `200` con todo sano; `503` con el directorio de fotos imposible.

## Cierre

- [x] `dotnet format` sin avisos nuevos y suite completa en verde.
- [x] Documentar el umbral en `appsettings.Example.json` y en `deploy/README.md`.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar y comprobar `/salud` contra producción.
- [ ] **Dar de alta el monitor externo** apuntando a `/salud`. Sin él, esta feature no sirve de nada: alguien tiene que mirar.

## Mantenimiento (checklist recurrente)

- [ ] Al añadir una dependencia externa nueva de la que dependa operar, decidir si entra en `/salud`. Lo que no está aquí, no lo vigila nadie.
