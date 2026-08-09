# 022 · Visor de fotos — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Componente

- [x] `VisorDeFotos.razor` — superposición con la foto completa.
- [x] Anterior y siguiente, ocultos si solo hay una foto. Dan la vuelta en los extremos.
- [x] Cerrar: botón, `Escape` y pulsación fuera de la foto.
- [x] Flechas del teclado para pasar de foto.
- [x] Indicador "foto N de M".
- [x] `role="dialog"`, `aria-modal`, y foco dentro al abrir.
- [x] Borrar desde el visor, solo si es del usuario, con la confirmación de siempre.
- [x] Iconos `cerrar`, `anterior` y `siguiente`.

## Estilos

- [x] Superposición, foto ajustada a la pantalla y controles legibles sobre la imagen.
- [x] Estados de carga y error propios: la clase `.foto` trae fondo claro, ilegible sobre el visor.
- [x] Tira de miniaturas en la ficha.

## Ficha

- [x] Pintar miniaturas en lugar de las fotos completas.
- [x] Abrir el visor al pulsar una, por su índice.
- [x] Quitar el botón de borrar de debajo de cada foto; pasa al visor.
- [x] Devolver el foco a la tira al cerrar.

## Pruebas

- [x] El visor pinta la foto por la que se abre, y pide **esa** a la API.
- [x] "Siguiente" cambia de foto y el indicador lo refleja.
- [x] "Anterior" desde la primera va a la última.
- [x] Con una sola foto, no hay controles de navegación.
- [x] El botón de cerrar avisa a quien lo abrió.
- [x] Sin permiso no se ofrece borrar; con permiso, borra **la que se está viendo** y cierra.
- [x] La ficha con fotos ofrece abrirlas.
- [x] La ficha ya **no** pide los archivos completos al cargarse.

Comprobado que muerde: devolviendo la ficha a las fotos completas falla
`ConFotos_LaFichaPideMiniaturasYNoLosArchivosCompletos`.

## Cierre

- [x] Suite completa en verde (597 pruebas, 0 fallos).
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [ ] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar.
- [ ] **Mirarlo en pantalla**, en el móvil y en el escritorio: los tests de bUnit no ven una superposición.

## Mantenimiento (checklist recurrente)

- [ ] Si algún día se añaden gestos o zoom, revisar antes si exigen JavaScript: la política de seguridad de contenido no permite `unsafe-inline`, y eso funciona en desarrollo y falla en producción.
