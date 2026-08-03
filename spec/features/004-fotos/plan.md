# 004 · Fotos — Plan

## Enfoque

Dos almacenes para una sola cosa: PostgreSQL guarda la referencia y los
metadatos, el disco guarda los bytes. Eso obliga a que las dos mitades no se
separen nunca, y define casi todo el diseño.

El acceso pasa siempre por la receta. Una foto no tiene permisos propios: hereda
los de su receta, y por eso todas las operaciones empiezan localizando la receta y
comprobando la autoría con `Receta.EsDe`, exactamente igual que en la 003.

El almacenamiento entra al dominio como puerto (`IAlmacenDeFotos`) porque la
constitución lo exige: si algún día se pasa a MinIO o S3, se cambia el adaptador y
nada más.

## Implementación

### Dominio (`Recetas.Dominio`)

1. `Recetas/TipoDeImagen.cs` — enumerado (`Jpeg`, `Png`, `Webp`) con su tipo de contenido y su extensión asociados.
2. `Recetas/Foto.cs` — entidad: `Id`, `RecetaId`, `TipoDeImagen`, `TamanoEnBytes`, `FechaDeSubida`. **No guarda el nombre del archivo**: la ruta se deriva del `Id`, así que no hay ningún texto del usuario en el camino.
3. `Receta` gana la colección `Fotos`, con `AnadirFoto` y `QuitarFoto`.
4. `Puertos/IAlmacenDeFotos.cs` — `GuardarAsync`, `AbrirAsync`, `BorrarAsync`. Habla de identificadores, nunca de rutas.

### Aplicación (`Recetas.Aplicacion`)

5. `Recetas/GestionDeFotos.cs` con `SubirAsync`, `ObtenerAsync` y `BorrarAsync`.
6. `Recetas/DetectorDeImagen.cs` — determina el tipo por los **bytes de cabecera**, no por lo que declare el cliente.
7. `GestionDeRecetas.BorrarAsync` pasa a borrar también los archivos de las fotos.

### Infraestructura (`Recetas.Infraestructura`)

8. `Fotos/AlmacenDeFotosEnDisco.cs` — un archivo por foto, con nombre `{Id}.{extensión}` dentro del directorio configurado.
9. `Fotos/OpcionesDeFotos.cs` — directorio y tamaño máximo.
10. Configuración EF de `Foto` con cascada desde la receta.
11. Migración `Fotos`.

### API (`Recetas.Api`)

12. Tres endpoints bajo `/recetas/{id}/fotos`, todos autenticados.
13. La respuesta de receta incluye los identificadores de sus fotos.
14. Límite de tamaño de la petición, que responde `413` en lugar de cortar la conexión.

## Decisiones

- **El nombre del archivo se deriva del identificador, no del que envía el cliente.** Un nombre de archivo controlado por el usuario es la vía clásica de travesía de rutas (`../../etc/algo`). Aquí el texto del cliente no llega a tocar el sistema de archivos: ni siquiera se guarda.
- **El tipo de imagen se detecta por los bytes de cabecera**, ignorando el `Content-Type` declarado. Fiarse de lo que dice el cliente permitiría subir cualquier cosa etiquetada como imagen y luego servirla con ese tipo, que es como se acaba sirviendo HTML —y por tanto JavaScript— desde el dominio de la API.
- **Se sirve con el tipo deducido del contenido y con `X-Content-Type-Options: nosniff`**, para que el navegador no reinterprete el archivo por su cuenta.
- **Nunca se sirve la carpeta como archivos estáticos.** Es un límite duro de la constitución y ya está anotado en la configuración de nginx: sería una puerta trasera de lectura que se salta el `401` y la comprobación de autoría.
- **Borrar la receta borra los archivos antes que las filas.** Si se hiciera al revés y fallara el borrado del archivo, quedarían archivos sin ninguna fila que los mencione: invisibles, imposibles de encontrar y ocupando disco para siempre. En este orden, un fallo deja la receta intacta y la operación se puede repetir.
- **Un fallo al borrar un archivo suelto no aborta la operación**, solo se registra. El caso contrario —dejar de borrar una receta porque un archivo ya no estaba— sería peor para el usuario.
- **La receta y sus fotos se cargan juntas.** Son pocas por receta y la ficha las necesita; separarlo obligaría a una segunda consulta en el único sitio donde siempre hacen falta.

## Riesgos

- **Filtración entre usuarios.** El riesgo central, igual que en la 003. Mitigación: el acceso pasa siempre por la receta y su comprobación de autoría, más tests explícitos.
- **Subida de contenido ejecutable disfrazado de imagen.** Mitigación: detección por bytes de cabecera, tipo de contenido derivado del servidor y `nosniff`.
- **Archivos huérfanos en disco.** Un fallo entre escribir el archivo y guardar la fila deja bytes sin referencia. Mitigación: se guarda la fila primero y el archivo después; si el archivo falla, se deshace la fila. El caso inverso —fila sin archivo— daría un error al leer, que es más visible que un archivo invisible.
- **Metadatos EXIF con ubicación GPS.** Ver el aviso de `spec.md`: acotado mientras todo es privado, **bloqueante para la 005**.
- **Agotar el disco.** No hay cuota por usuario. Mitigación hoy: límite por archivo. Anotado como algo a vigilar; una cuota real es trabajo de otra feature.
