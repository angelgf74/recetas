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

**Tampoco lleva AdMob**, y no es un olvido: exige cuenta de publicador,
identificador real, política de privacidad publicada, declaración de datos en
Google Play y una plataforma de consentimiento para la UE. Nada de eso es código.
Ver `spec/features/012-android-esqueleto/spec.md`.
