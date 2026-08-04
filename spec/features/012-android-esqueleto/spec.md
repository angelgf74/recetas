# 012 · Android: esqueleto usable

**Estado:** hecho

## Qué hace

Una aplicación Android nativa (Kotlin + Compose) contra la misma API: iniciar
sesión, ver el recetario propio, abrir una receta y buscar.

## Por qué

`mission.md` dice que el caso de uso central es **consultar la receta mientras se
cocina**, con el móvil en la encimera. Hoy eso lo cubre la web responsive, que
funciona pero exige abrir el navegador y escribir una dirección. Una aplicación
instalada se abre de un toque y recuerda la sesión.

## Alcance: por qué solo el esqueleto

La aplicación completa son once features de web más la publicidad. Se parte igual
que se partió la web, y por el mismo motivo que `roadmap.md` ya dejó escrito: la
**002** levantó el esqueleto y la **007** lo completó cuando la API estaba entera.

Esta feature entrega lo que hace la aplicación **útil desde el primer día**:
leer. Crear y editar recetas, fotos, publicar e importar llegan después; para eso
está la web, que funciona en el móvil.

**AdMob queda fuera de esta feature**, y es deliberado:

- Exige cuenta de publicador, identificador de aplicación real, política de
  privacidad publicada, declaración de datos en Google Play y una plataforma de
  consentimiento para la UE. Nada de eso es código.
- `mission.md` dice que la publicidad no manda sobre el producto. Ponerla antes
  de que la aplicación sirva para algo sería justo lo contrario.

## Criterios de aceptación

### Proyecto

- [x] El proyecto vive en `android/`, aparte de la solución .NET, y compila con el envoltorio de Gradle.
- [x] `gradlew assembleDebug` produce un APK (13 MB).
- [x] La ruta del SDK **no** se guarda en el repositorio: cada máquina tiene la suya.
- [x] No se añade nada de Android a `Recetas.slnx`: son dos cadenas de construcción distintas.

### Sesión

- [x] Hay una pantalla de inicio de sesión que llama a `POST /sesiones`.
- [x] El token se guarda cifrado con una clave del almacén del sistema, que no sale del dispositivo. Además, `allowBackup="false"`: la copia automática se llevaría el token a los servidores de Google.
- [x] La sesión sobrevive a cerrar y volver a abrir la aplicación. **Probado en el emulador** parando la aplicación del todo.
- [x] Toda petición lleva la cabecera `Authorization`, puesta en un solo sitio, con test.
- [x] Un `401` cierra la sesión y devuelve al inicio de sesión, desde cualquier pantalla. En el inicio de sesión, en cambio, un `401` significa "credenciales incorrectas": también con test.
- [x] Se puede cerrar sesión, y al hacerlo el token desaparece del dispositivo.

### Leer

- [x] La pantalla principal lista el recetario propio con su miniatura.
- [x] Tocar una receta abre su ficha: ingredientes, elaboración y fotos.
- [x] Hay búsqueda por nombre e ingredientes contra `GET /recetas/busqueda`.
- [x] Las imágenes se piden con la cabecera de autorización: el endpoint la exige.

### Comportamiento en el móvil

- [x] Se ve bien en vertical y sobrevive a girar la pantalla sin perder lo cargado. **Probado girando el emulador.**
- [x] Sin red, se dice qué ha pasado en lugar de quedarse en blanco. **Visto de verdad**: el primer intento contra la API no llegó y la pantalla mostró el aviso y recuperó el botón.
- [x] La ficha **no deja que la pantalla se apague** mientras está abierta. Implementado; el efecto en sí **no se ha comprobado a ojo** —haría falta esperar al tiempo de apagado del dispositivo—.

### Contrato

- [x] La aplicación **no** usa endpoints propios: consume los mismos que la web.
- [x] Los modelos de datos se corresponden con `Recetas.Contratos`, y toleran campos nuevos para que una versión del servidor no rompa una aplicación ya instalada.

### Calidad

- [x] Hay tests de las piezas con lógica propia, y pasan con `gradlew test` (11 tests).
- [x] La aplicación compila sin avisos. Los que salieron —`EncryptedSharedPreferences` está obsoleto— se quitaron cifrando a mano contra el almacén de claves, lo que además eliminó una dependencia.

## Fuera de alcance

- **Crear, editar y borrar recetas**, fotos, publicar/despublicar, escalar
  cantidades e importar desde URL. Todo eso ya está en la API y lo cubre la web;
  entra en la feature siguiente de Android.
- **AdMob.** Ver arriba.
- **Alta de cuenta y recuperar contraseña desde la aplicación.** El alta necesita
  un enlace de correo que aterriza en la web; duplicar ese flujo en Android antes
  de tener enlaces profundos es trabajo tirado.
- **Modo sin conexión.** Guardar recetas en el dispositivo para leerlas sin red es
  una feature entera, con su sincronización y sus conflictos.
- **Publicación en Google Play.**
