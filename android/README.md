# Aplicación Android

Cliente nativo (Kotlin + Compose) de la misma API que la web. Ver
`spec/features/012-android-esqueleto/`.

Proyecto Gradle **aparte de la solución .NET**: son dos cadenas de construcción
que no se hablan. `dotnet build` no compila esto, y `gradlew` no compila aquello.

## Qué hace falta

- **Android Studio**, por su SDK y por su JDK.
- **JDK 21, el que trae Android Studio.** No vale el JDK del sistema si es más
  nuevo: el complemento de Gradle para Android no sigue el ritmo de Java.

  ```powershell
  $env:JAVA_HOME = "$env:ProgramFiles\Android\Android Studio\jbr"
  ```

- **`local.properties` con la ruta del SDK.** No está en el repositorio porque es
  de cada máquina. Android Studio lo crea al abrir el proyecto; a mano:

  ```
  sdk.dir=C:\\Users\\TUUSUARIO\\AppData\\Local\\Android\\Sdk
  ```

## Comandos

```powershell
.\gradlew.bat test            # tests de las piezas con lógica
.\gradlew.bat assembleDebug   # APK en app/build/outputs/apk/debug/
.\gradlew.bat installDebug    # compila e instala en el dispositivo conectado
```

## Apuntar a la API local

La compilación de depuración apunta a `http://localhost:5199/`, y la de
publicación a `https://recetas-api.angelgf.com.es/`. Lo fija `BASE_DE_LA_API` en
`app/build.gradle.kts`.

Para que `localhost` dentro del teléfono o del emulador sea el de tu equipo:

```powershell
adb reverse tcp:5199 tcp:5199
```

**Por qué así y no con `10.0.2.2`**, que es la dirección con la que el emulador ve
al anfitrión: esa entra por la red y la para el cortafuegos de Windows, cuya
apertura exige permisos de administrador. `adb reverse` va por el canal de
depuración, no toca nada, y además funciona igual con un teléfono real por cable.

Hay que rehacerlo cada vez que se reconecta el dispositivo.

## Probar en el emulador

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd NOMBRE_DEL_AVD
adb wait-for-device
adb reverse tcp:5199 tcp:5199
.\gradlew.bat installDebug
adb shell am start -n com.angelgf.recetas/.MainActivity
```

Y la API local, escuchando y con la base de datos en pie:

```powershell
docker compose up -d
dotnet run --project ..\src\Recetas.Api
```

## Lo que NO hace todavía

Crear y editar recetas, fotos, publicar, escalar cantidades e importar desde URL.
Todo eso está en la API y lo cubre la web, que funciona en el móvil. Entra en la
feature siguiente de Android.

**AdMob está integrado en el código** (feature 013), pero **todavía no sale ningún
anuncio**: falta crear el mensaje de consentimiento del RGPD en la consola. Ver
más abajo.

## AdMob

Identificadores ya creados en la consola. **No son secretos**: acaban dentro del
APK y cualquiera puede leerlos, así que se versionan.

| Qué | Identificador |
| --- | --- |
| Aplicación | `ca-app-pub-8600791204816041~1145017083` |
| Banner del recetario | `ca-app-pub-8600791204816041/2095402606` |
| Banner de la búsqueda | `ca-app-pub-8600791204816041/9782320932` |

**Dónde van y dónde no.** Banner al pie del recetario y de la búsqueda. **Ninguno
en la ficha de receta**: es la pantalla que se lee cocinando, y `mission.md` dice
que un anuncio que estorbe ahí se quita. Por lo mismo no hay intersticial.

Un bloque por pantalla en vez de uno compartido, para poder medir cuál rinde y
retirar el que moleste sin tocar el otro.

La aplicación figura como **"Debe revisarse"** y con servicio de anuncios
limitado, porque no está publicada en ninguna tienda. Es lo esperado hasta que se
suba a Google Play.

### En depuración se usan bloques de prueba, y no es opcional

Servir o pulsar anuncios **reales** mientras se desarrolla genera lo que AdMob
considera tráfico inválido, y la consecuencia habitual no es un aviso: es la
**suspensión de la cuenta de publicador**, que tiene otras diez aplicaciones
dentro.

Por eso hay dos defensas, a propósito:

1. La compilación de depuración usa los **bloques de prueba públicos de Google**;
   los reales solo entran en la de publicación (`app/build.gradle.kts`).
2. El emulador se registra como **dispositivo de prueba** en `Anuncios.kt`.

Si vas a probar en un teléfono propio, añade su identificador a esa lista: sale en
el logcat la primera vez que se pide un anuncio.

### Lo que bloquea que salgan anuncios

Hoy no sale ninguno. UMP responde:

```
no form(s) configured for the input app ID
```

Falta crear el mensaje de consentimiento en **AdMob → Privacidad y mensajes →
Reglamentos europeos → Crear**. Hasta entonces el código no pide anuncios, que es
el comportamiento correcto.

### Lo demás que falta

- **Política de privacidad**: actualizarla cuando los anuncios se sirvan de
  verdad. Hoy dice que no hay publicidad, y es cierto.
- **Declaración de datos** en Google Play, marcando que la aplicación contiene
  anuncios.
