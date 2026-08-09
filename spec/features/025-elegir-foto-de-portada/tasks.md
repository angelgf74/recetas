# 025 · Elegir la foto de portada — Tareas

## Dominio

- [x] `Receta.FotoDePortadaElegidaId`, `ElegirFotoDePortada`, `FotoDePortada` actualizado, `QuitarFoto` limpia la referencia.

## Infraestructura

- [x] Columna y FK en `ConfiguracionDeReceta` (`ON DELETE SET NULL`), migración `20260809140918_FotoDePortada` — sin problemas de EF con la referencia cruzada `recetas` ↔ `fotos`.

## Aplicación

- [x] `GestionDeFotos.ElegirPortadaAsync`.

## API

- [x] `PUT /recetas/{id}/fotos/{fotoId}/portada`.
- [x] `FotoRespuesta.EsPortada`.

## Web

- [x] Botón "Hacer portada" en `VisorDeFotos`, solo para el autor y solo si no lo es ya.
- [x] Insignia de portada en la miniatura de la ficha.
- [x] `ClienteDeApi.ElegirPortadaAsync`.

## Pruebas

- [x] Dominio: elegir portada la cambia; elegir la misma no rompe nada; borrar la elegida vuelve a la derivada.
- [x] Dominio: elegir una foto que no es de la receta falla.
- [x] Dominio: borrar la elegida limpia también `FotoDePortadaElegidaId` en sí, no solo lo derivado.
- [x] Aplicación/integración: solo el autor puede elegir portada; elegir una foto de otra receta falla.
- [x] Integración: ida y vuelta por la API; se refleja en `RespuestaDeReceta.Fotos[].EsPortada`.

Comprobado: quitar la limpieza en `QuitarFoto` **no** hace fallar
"borrar la portada vuelve a la derivada" — ni en dominio ni en integración—,
porque hay dos redes independientes que ya cubren ese caso: el `??` de
`FotoDePortada` en el dominio, y la relación de EF configurada con
`OnDelete(SetNull)`, que hace *fixup* del lado cliente aunque la base de
datos nunca llegue a intervenir. El test que sí muerde sin la limpieza es
`BorrarLaElegida_LimpiaLaEleccionEnSi`, que mira `FotoDePortadaElegidaId`
directamente y no lo derivado: es la única forma de comprobar que el
agregado no se queda con una referencia colgada por dentro, aunque nada de
lo observable desde fuera lo note.

## Cierre

- [x] Suite completa en verde (653 pruebas, 0 fallos).
- [ ] `CLAUDE.md`, `roadmap.md`.
- [ ] Desplegar.
