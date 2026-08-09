# 026 · Convertir unidades al escalar — Tareas

## Dominio

- [x] `EscaladoDeCantidades.EscalarConUnidad` — nuevo, `Escalar` intacto.
- [x] `Receta.EscalarA` usa `EscalarConUnidad` solo cuando `PuedeEscalarseA` es verdad.

## Pruebas

- [x] Dominio: 500 g doblados a 1000 → "1 kg". Mismo caso para mililitros/litros.
- [x] Dominio: por debajo de 1000 sigue en gramos/mililitros.
- [x] Dominio: la conversión redondea en la unidad de destino (cuartos de kilogramo), no dos veces.
- [x] Dominio: unidades sin conversión (cucharada, contables…) no cambian de unidad.
- [x] Dominio/Aplicación: **sin pedir raciones, la unidad guardada no cambia** — ni en la ficha en reposo ni en lo que precarga la edición.
- [x] Integración: `GET /recetas/{id}?raciones=N` que cruza el umbral devuelve la unidad convertida; sin el parámetro, no.

Comprobado que muerden: forzando `conConversionDeUnidad = true` sin
condición en `EscalarA` fallan `SinPedirRaciones_NoConvierteDeUnidadAunqueSuperaraElUmbral`
(dominio) y `SinPedirRaciones_NoConvierteAunqueElValorGuardadoSuperaraElUmbral`
(integración) — el caso que de verdad importaba proteger.

## Cierre

- [x] Suite completa en verde (669 pruebas, 0 fallos).
- [x] `CLAUDE.md`, `roadmap.md`.
- [ ] Desplegar.
