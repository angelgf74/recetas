# 019 · Exportar mis datos

**Estado:** implementado ✅

## Qué hace

Un usuario puede **descargar todo lo que Recetas guarda de él** en un solo archivo: sus recetas con ingredientes y elaboración, sus fotos tal cual las subió, y los datos de su cuenta.

Se pide desde la misma pantalla que la baja, y llega como un `.zip` con un `datos.json` legible y una carpeta de fotos.

## Por qué

**Es el derecho de portabilidad del RGPD**, que la 016 dejó fuera a propósito. Hoy se atiende escribiendo un correo y a mano: eso funciona con tres usuarios y deja de funcionar con treinta.

Y hay una razón que no es legal: **irse sin poder llevarse nada es una puerta cerrada con llave**. Un recetario personal donde el usuario no puede recuperar sus propias recetas es exactamente lo que `mission.md` promete que no pasará al decir que *"el autor manda sobre sus datos"*.

Conviene tenerlo **antes** de que alguien lo pida, no después. Con la aplicación ya publicada, ese alguien puede aparecer cualquier día.

## Criterios de aceptación

- [x] Un usuario autenticado puede descargar un `.zip` con **sus** datos. — El endpoint está probado de extremo a extremo. **El botón de la web no se ha visto funcionar en un navegador**: entrega el archivo con un objeto `blob`, y si la política de seguridad de contenido lo bloqueara habría que añadirle `blob:`. Pendiente de comprobar.
- [x] Sin sesión responde `401`.
- [x] El archivo incluye un `datos.json` con la cuenta y **todas** sus recetas: nombre, tipo de plato, elaboración, raciones, visibilidad, fechas e ingredientes con cantidad y unidad.
- [x] Incluye **las fotos originales**, no las miniaturas, y el `datos.json` dice a qué receta pertenece cada archivo.
- [x] Incluye un `LEEME.txt` que explique qué hay dentro, en castellano y sin tecnicismos.
- [x] **No incluye datos de otros usuarios**, ni siquiera de recetas públicas ajenas.
- [x] **No incluye el hash de la contraseña** ni ningún secreto: no son datos que le sirvan a nadie y sí un riesgo si el archivo se pierde.
- [x] El paquete se escribe **según se genera**, sin cargar todas las fotos en memoria a la vez.
- [x] El endpoint está **limitado por frecuencia**: generarlo cuesta leer todo el disco del usuario.
- [x] Una cuenta **sin recetas** produce un archivo válido, con su `datos.json` y sin fotos.

## Fuera de alcance

- **Generación en segundo plano con aviso por correo.** Haría falta una cola de trabajos y almacenamiento temporal. Con los tamaños de este producto —decenas de recetas y unas pocas fotos— la descarga directa basta; si algún día no basta, se nota en el tiempo de respuesta y entonces se hace.
- **Importar lo exportado.** Sería otra feature, y una que además abre la puerta a subir archivos preparados por terceros.
- **Formatos de recetario estándar** (`schema.org/Recipe`, Paprika, Mealie). El RGPD pide un formato estructurado y de uso común, y JSON lo es. La interoperabilidad con otras aplicaciones es otra conversación.
- **Exportar desde Android.** La web basta para un caso que se usa una vez en la vida, y en el móvil un `.zip` es incómodo de manejar. Si se pide, se añade.
