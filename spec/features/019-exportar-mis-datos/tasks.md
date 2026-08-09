# 019 · Exportar mis datos — Tareas

_Checklist accionable derivada del `plan.md`. Tareas pequeñas y concretas; marca `[x]` al completarlas._

## Aplicación

- [x] `ExportarMisDatos` — reúne cuenta, recetas con ingredientes y fotos.
- [x] Apertura de cada foto sin que quien empaqueta conozca el almacén.

## Contratos

- [x] `DatosExportados` — la forma del `datos.json`, con tipos propios del paquete.

## API

- [x] `GET /yo/datos`, autenticado, escribiendo el ZIP sobre `Response.Body`.
- [x] `LEEME.txt` dentro del paquete, en castellano y sin tecnicismos.
- [x] Límite de frecuencia propio, con ventana generosa.

## Web

- [x] Enlace de descarga en `MiCuenta`, **antes** del bloque de baja.

## Pruebas

- [x] Aplicación: reúne lo del usuario y **nada ajeno**.
- [x] Integración: `401` sin sesión.
- [x] Integración: `200` con un ZIP legible que contiene `datos.json`, `LEEME.txt` y las fotos.
- [x] Integración: las recetas y fotos **de otro usuario no aparecen**, ni siquiera las públicas.
- [x] Integración: una cuenta **sin recetas** produce un archivo válido.
- [x] Integración: el `datos.json` **no contiene el hash de la contraseña**.

## Cierre

- [x] `dotnet format` sin avisos nuevos y suite completa en verde.
- [x] Actualizar `privacidad.html`: la portabilidad ya no se atiende solo por correo.
- [x] Validar contra los criterios de aceptación de `spec.md`.
- [x] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.
- [ ] Desplegar y comprobar la descarga contra producción con la cuenta de demostración.

## Mantenimiento (checklist recurrente)

- [ ] Al añadir un dato nuevo del usuario, decidir si entra en la exportación. Lo que no esté aquí no se lo puede llevar, aunque sea suyo.
