# 014 · Android: paridad con la web

**Estado:** en curso

## Qué hace

La aplicación Android pasa de solo leer a hacer **todo lo que hace la web**:
crear, editar y borrar recetas, gestionar fotos, publicar, escalar cantidades por
comensales, importar desde una URL, darse de alta y recuperar la contraseña.

Los anuncios de la 013 siguen donde estaban: recetario y búsqueda, nunca en la
ficha.

## Por qué

La 012 entregó el esqueleto de lectura a propósito, dejando escribir para la web.
Con la aplicación ya instalada en el móvil, tener que abrir el navegador para
añadir una receta es exactamente la fricción que `mission.md` quiere evitar.

## Criterios de aceptación

### Escribir recetas

- [ ] Crear una receta: nombre, tipo de plato, raciones, ingredientes y elaboración.
- [ ] Editar una receta propia, con los campos precargados.
- [ ] Borrar una receta propia, con confirmación.
- [ ] Los ingredientes se añaden y se quitan uno a uno, con cantidad y unidad.
- [ ] No se puede guardar una receta sin nombre, sin elaboración o sin ingredientes.
- [ ] Las raciones son opcionales, entre 1 y 100.

### Fotos

- [ ] Añadir una foto a una receta propia, eligiéndola del dispositivo.
- [ ] Borrar una foto, con confirmación.
- [ ] La ficha muestra todas las fotos, no solo la primera.

### Compartir

- [ ] Publicar y despublicar una receta propia.
- [ ] El estado se ve en la ficha.
- [ ] Sobre una receta ajena publicada **no se ofrece** ninguna acción de escritura.

### Escalar cantidades

- [ ] La ficha de una receta con raciones permite cambiar el número de comensales.
- [ ] Se ve cuándo lo mostrado no son las cantidades guardadas, y se puede volver.

### Importar desde URL

- [ ] Pegar un enlace rellena el formulario de receta nueva.
- [ ] Se avisa de que hay que revisar lo importado y de dónde viene.

### Cuenta

- [ ] Alta desde la aplicación: se pide el correo y se avisa de que hay que abrir el enlace.
- [ ] Recuperar contraseña desde la aplicación, igual.
- [ ] El enlace del correo **abre la aplicación** si está instalada, y completa el paso 2 dentro de ella.
- [ ] Si la aplicación no está instalada, el enlace sigue funcionando en la web.

### Lo que no cambia

- [ ] Los anuncios siguen sin aparecer en la ficha de receta.
- [ ] La API no gana ni un endpoint: se consumen los mismos que la web.

### Calidad

- [ ] `gradlew assembleDebug` y `gradlew test` en verde, sin avisos.
- [ ] Probado en un dispositivo real contra producción.

## Fuera de alcance

- **Modo sin conexión.**
- **Cambiar la contraseña estando dentro**, que tampoco tiene la web.
- **Cámara**: las fotos se eligen de la galería, no se hacen desde la aplicación.
