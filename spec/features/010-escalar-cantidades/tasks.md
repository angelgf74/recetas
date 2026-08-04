# 010 · Escalar cantidades — Tareas

## Dominio

- [x] `Receta.Raciones` (`int?`), con rango, en `Crear` y `Actualizar`.
- [x] `EscaladoDeCantidades`: la tabla de redondeos por unidad.
- [x] `Receta.EscalarA(raciones)`, que devuelve una proyección y no muta.
- [x] `LineaEscalada`, tipo aparte de la entidad para que nadie la persista por error.

## Aplicación

- [x] `DatosDeReceta` lleva las raciones a `Crear` y `Actualizar`.

## Infraestructura

- [x] Configuración EF y migración `Raciones` (columna nula, sin relleno).

## API

- [x] `GET /recetas/{id}?raciones=N`, con validación de rango.
- [x] `Raciones` y `RacionesMostradas` en `RespuestaDeReceta`; `Raciones` en `PeticionDeReceta`.

## Web

- [x] Campo de raciones en el formulario.
- [x] Control de comensales en la ficha, con aviso y vuelta a las originales.

## Validación

- [x] Test por cada regla de redondeo (contables, gramos, resto).
- [x] Test de que `AlGusto` y `Pizca` no escalan.
- [x] Test de que ninguna cantidad escalada se queda en cero.
- [x] Test de que pedir las mismas raciones devuelve las cantidades exactas.
- [x] Test de que escalar no toca la base de datos.
- [x] Test de que `raciones` fuera de rango responde `400`.
- [x] Test de coherencia del rango entre dominio y contratos.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
