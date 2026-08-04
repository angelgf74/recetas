# 012 · Android: esqueleto usable — Plan

## Enfoque

Un proyecto Gradle en `android/`, separado de la solución .NET. Son dos cadenas
de construcción que no se hablan: `dotnet build` no debe intentar compilar Kotlin
ni al revés.

La aplicación es **un cliente más de la misma API**, exactamente igual que la web.
`tech-stack.md` ya lo exige: "la API se diseña como si Android ya existiera; nada
de endpoints a medida". Esta feature es donde esa regla se cobra o se paga: si
hiciera falta un endpoint nuevo para que Android funcione, significaría que el
contrato estaba mal, y se arregla el contrato para todos.

## Estructura

```
android/
  settings.gradle.kts
  build.gradle.kts
  gradle/libs.versions.toml      catálogo de versiones
  app/
    build.gradle.kts
    src/main/java/com/angelgf/recetas/
      datos/        cliente HTTP, modelos, almacén de sesión
      ui/           pantallas Compose y sus modelos de vista
      MainActivity.kt
    src/test/java/  tests de las piezas con lógica
```

## Decisiones

- **Ktor Client, no Retrofit.** Ktor es multiplataforma, ya usa
  `kotlinx.serialization` y trae interceptores sin necesidad de OkHttp aparte.
  Menos dependencias que descargar y menos capas que entender.
- **El token, en `EncryptedSharedPreferences`.** Un `SharedPreferences` normal es
  un XML legible en un teléfono con root o con copia de seguridad activada. La web
  usa `localStorage` porque el navegador no da más; aquí sí lo da.
- **La cabecera se pone en el cliente HTTP, no en cada llamada**, y el `401` se
  atrapa en el mismo sitio. Es la misma decisión que `ManejadorDeAutenticacion` en
  la web, y por el mismo motivo: que ninguna pantalla pueda olvidarse.
- **Las imágenes se descargan con el cliente autenticado.** No sirve una biblioteca
  de carga de imágenes apuntada a la URL a secas: el endpoint de fotos exige
  cabecera. Se piden como bytes y se decodifican.
- **Modelos propios que reflejan `Recetas.Contratos`.** No hay forma de compartir
  tipos entre C# y Kotlin sin generar código, y generar código exigiría un paso
  más en la construcción para ahorrar cuatro clases de datos. Se escriben a mano y
  la spec deja dicho que, si divergen, se arregla Android.
- **`ViewModel` + `StateFlow`.** Es lo que sobrevive al giro de pantalla, que es un
  criterio de aceptación y el fallo clásico de una primera app.
- **La ficha mantiene la pantalla encendida.** Cocinar con las manos ocupadas es el
  caso de uso central de `mission.md`; que se apague a los treinta segundos lo
  rompe. Solo en la ficha: dejarla encendida en toda la aplicación gastaría batería
  sin motivo.
- **`local.properties` fuera del repositorio.** Lleva la ruta del SDK, que es de
  cada máquina.
- **JDK 21 y no el 25 del sistema.** El complemento de Gradle para Android no
  sigue el ritmo de las versiones de Java; Android Studio trae su propio JDK 21 y
  es el que se usa.

## Implementación

1. Envoltorio de Gradle, `settings.gradle.kts`, catálogo de versiones y `.gitignore`.
2. `SesionLocal` — guarda y borra el token cifrado.
3. `ClienteDeApi` — Ktor con la cabecera, el `401` centralizado y la traducción de errores.
4. Modelos de datos espejo del contrato.
5. Pantallas: inicio de sesión, recetario, ficha, búsqueda.
6. Navegación y `MainActivity`.
7. Tests de `SesionLocal` y de la traducción de errores.

## Riesgos

- **La primera construcción descarga medio internet.** Gradle, el complemento de
  Android y Compose. No hay forma de evitarlo; si la red falla, no se compila.
- **Versiones del complemento de Android contra JDK 25.** Mitigado usando el JDK
  que trae Android Studio.
- **No hay dispositivo donde probar de verdad.** Se puede compilar y correr los
  tests; ver la aplicación funcionando exige un emulador o un teléfono, y eso hay
  que decirlo en lugar de dar por buena una pantalla que nadie ha visto.
- **La API de producción exige HTTPS con certificado válido.** No hay que tocar la
  configuración de seguridad de red para llegar a ella, pero sí para apuntar a la
  API local durante el desarrollo.
