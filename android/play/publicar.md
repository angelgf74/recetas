# Cómo publicar Recetas en Google Play

Lista de lo que falta, **en orden de dependencia**. Cada bloque dice quién puede
hacerlo: hay cosas que son código y otras que exigen decisiones o una cuenta.

> Nada de esto es asesoramiento legal. Los apartados marcados con ⚖ conviene que
> los revise alguien que sepa, sobre todo si Recetas deja de ser un proyecto
> personal.

---

## 1. Mensaje de consentimiento del RGPD en AdMob

**Por qué:** sin él no se pueden servir anuncios personalizados a usuarios del
Espacio Económico Europeo, Reino Unido y Suiza. Hoy los anuncios de prueba salen
igual, así que **no bloquea el desarrollo, pero sí la publicación**.

Datos que vas a necesitar a mano:

| Qué | Valor |
| --- | --- |
| Aplicación | Recetas |
| ID de aplicación | `ca-app-pub-8600791204816041~1145017083` |
| Política de privacidad | `https://recetas.angelgf.com.es/privacidad.html` |

### Paso 1 · Entrar

[apps.admob.com](https://apps.admob.com) → menú lateral **Privacidad y mensajes**
→ tarjeta **Reglamentos europeos** → **Crear**.

### Paso 2 · Elegir a qué aplicaciones se aplica

Marca **Recetas**. Puedes marcar también el resto de tus aplicaciones si quieres
un único mensaje para todas; el mensaje es por cuenta, no por aplicación, y se
asigna a las que elijas.

### Paso 3 · Proveedores de anuncios

**Esta es la decisión de verdad, y es tuya.** Google presenta la lista de
terceros que podrán tratar datos de tus usuarios si estos consienten.

- Menos proveedores: menos empresas con datos de tus usuarios, menos competencia
  por cada hueco, menos ingresos.
- Más proveedores: al revés.

Google trae marcada una selección por defecto. Si no tienes criterio propio,
déjala: es la que usan la mayoría de publicadores. Lo que **no** conviene es
marcarlos todos sin mirar.

### Paso 4 · Opciones del mensaje

- **Idiomas:** añade **español**. Si solo dejas inglés, tus usuarios verán el
  formulario en inglés, y un consentimiento que el usuario no entiende no es
  consentimiento válido.
- **Botones:** tiene que haber una forma de **rechazar tan accesible como la de
  aceptar**. Google lo exige desde 2024 y es lo que hace válido el consentimiento
  bajo el RGPD. Si el asistente ofrece "Consentir / Gestionar opciones", añade
  también **"No consentir"**.
- **Enlace a la política de privacidad:** pega
  `https://recetas.angelgf.com.es/privacidad.html`.

### Paso 5 · Publicar

**Guardar no basta: hay que pulsar Publicar.** Un mensaje creado y sin publicar
sigue dando el mismo error que ahora.

### Paso 6 · Comprobar que funciona

Con el móvil conectado:

```powershell
adb logcat -c
adb shell am start -n com.angelgf.recetas/.MainActivity
adb logcat -d -s Anuncios:*
```

- **Antes:** `No se pudo obtener el consentimiento: … no form(s) configured for
  the input app ID`.
- **Bien:** `Consentimiento consultado. ¿Se pueden pedir anuncios? true`, y el
  formulario aparece en pantalla.

Puede tardar **hasta una hora** en propagarse. Si sigue igual pasado ese rato,
revisa que el mensaje esté *publicado* y asignado a *Recetas*.

### El formulario solo sale una vez

Una vez respondes, UMP guarda la respuesta y no vuelve a preguntar. Para volver a
verlo mientras pruebas:

**Ajustes → Aplicaciones → Recetas → Almacenamiento → Borrar datos.**

Eso borra también la sesión, así que habrá que iniciar sesión otra vez.

> Si esto se va a repetir mucho, se puede añadir a la compilación de depuración
> una llamada a `ConsentInformation.reset()`. No está puesto porque un botón que
> borra el consentimiento no debe existir en la versión publicada.

---

## 2. ~~⚖ Identidad del responsable del tratamiento~~ · HECHO

La política identifica como responsable a **Ángel Galán Fernández**, persona
física, con `angelgf@gmail.com` como contacto para ejercer derechos.

**Si Recetas dejara de ser un proyecto personal** —una sociedad, ingresos
significativos por publicidad, usuarios más allá del círculo cercano—, conviene
revisarlo con alguien que sepa: cambiaría lo que hay que publicar (denominación
social, NIF, domicilio) y probablemente haga falta también un registro de
actividades de tratamiento.

---

## 3. ~~Capturas de pantalla de verdad~~ · HECHO

Las cuatro capturas de `capturas/` están hechas en un teléfono real con recetas y
fotos de verdad, y recortadas a 1220×2440.

**Si alguna vez se rehacen, cuidado con la proporción:** Play admite como máximo
**2:1**, y la pantalla nativa de un móvil moderno suele ser más alargada (2,22:1
en el Redmi con el que se hicieron). Sin recortar, Play las rechaza. El detalle
está en `ficha.md`.

---

## 4. Cuenta de desarrollador de Google Play

**Por qué:** sin ella no hay dónde subir nada.

**Dónde:** [Play Console](https://play.google.com/console). Cuota única de unos
25 USD. Si ya tienes una —tienes diez aplicaciones publicadas—, este paso está
hecho.

---

## 5. Firmar la aplicación

**Por qué:** Play solo acepta un AAB firmado, y **la firma es para siempre**: si
pierdes el almacén de claves, no puedes volver a actualizar la aplicación nunca
más.

**Crear el almacén** (una sola vez, guárdalo donde no se pierda y **fuera del
repositorio**):

```powershell
& "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe" `
    -genkeypair -v `
    -keystore C:\ruta\segura\recetas-release.jks `
    -keyalg RSA -keysize 2048 -validity 10000 `
    -alias recetas
```

**Configurar Gradle.** En `android/keystore.properties` (ignorado por git):

```
storeFile=C:/ruta/segura/recetas-release.jks
storePassword=…
keyAlias=recetas
keyPassword=…
```

Dime cuando lo tengas y cableo el `signingConfig` en `build.gradle.kts`. **No me
mandes las contraseñas por el chat**: quedarían escritas en el historial. Ponlas
tú en el archivo.

**Generar el AAB:**

```powershell
.\gradlew.bat bundleRelease
```

Sale en `app/build/outputs/bundle/release/app-release.aab`.

---

## 6. Añadir la huella de publicación a assetlinks.json

**Por qué:** los enlaces del correo del alta y de recuperar contraseña abren la
aplicación gracias a `/.well-known/assetlinks.json`, que hoy solo lleva la huella
del certificado de **depuración**. La aplicación descargada de Play tendrá otra
firma, y **los enlaces dejarán de abrirla**.

**Sacar la huella del certificado de publicación:**

```powershell
& "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe" `
    -list -v -keystore C:\ruta\segura\recetas-release.jks -alias recetas
```

Copia la línea `SHA256:`.

**Cuidado:** si activas la **firma de aplicaciones de Google Play** —lo habitual
hoy—, la huella que importa es la que Play te muestra en *Configuración → Integridad
de la aplicación*, no la tuya. Usa esa.

Añádela al array `sha256_cert_fingerprints` de
`src/Recetas.Web/wwwroot/.well-known/assetlinks.json`, **sin quitar la de
depuración**, y despliega. Pueden convivir.

---

## 7. Ficha de la tienda

Los textos están escritos en `ficha.md`: título, descripción corta, descripción
completa y novedades. Cópialos tal cual.

**Recuerda que la descripción avisa de que esta versión ya hace todo lo que hace
la web.** Si publicas antes de que eso sea cierto en la versión subida, hay que
cambiarla: prometer de más infringe las políticas de Play.

---

## 8. Cuestionario de clasificación de contenido

**Dónde:** Play Console → **Contenido de la aplicación** → **Clasificación de
contenido**.

Para una aplicación así, las respuestas suelen ser "no" a todo: sin violencia, sin
sexo, sin drogas, sin juego, sin compras. **Sí** hay contenido generado por
usuarios (las recetas públicas), y eso puede pedir que declares que existe un
mecanismo de moderación o denuncia. Hoy **no lo hay**, así que prepárate para
responder a eso; puede que te obliguen a añadirlo.

---

## 9. Seguridad de los datos

**Dónde:** Play Console → **Contenido de la aplicación** → **Seguridad de los
datos**.

Con lo que la aplicación hace de verdad, las respuestas son:

| Pregunta | Respuesta |
| --- | --- |
| ¿Recoge datos? | Sí |
| Correo electrónico | Sí, para gestión de la cuenta. Obligatorio. |
| Contenido del usuario (recetas y fotos) | Sí, funcionalidad de la aplicación. Obligatorio. |
| ¿Se comparte con terceros? | Sí, con la red publicitaria |
| ¿Cifrado en tránsito? | Sí |
| ¿Se puede solicitar el borrado? | Sí, escribiendo al correo de contacto |
| Identificadores publicitarios | Sí, por AdMob |
| Ubicación, contactos, agenda, micrófono | No |

**Todo eso está respaldado por la política de privacidad publicada.** Si cambias
una respuesta aquí, cambia también la política.

---

## 10. Declarar que contiene anuncios

**Dónde:** Play Console → **Contenido de la aplicación** → **Anuncios**.

Responde **sí**. Aunque hoy solo salgan anuncios de prueba, la versión publicada
llevará reales. Declarar que no y luego mostrarlos es una infracción.

---

## Orden recomendado

1. Mensaje de consentimiento en AdMob (1) — es lo único que ya deberías poder
   hacer hoy, y desbloquea probar anuncios de verdad.
2. Identidad del responsable (2) — cinco minutos, y sin ello la política está coja.
3. Capturas (3).
4. Almacén de claves y AAB (5), y con eso la huella (6).
5. Todo lo de Play Console (4, 7, 8, 9, 10).

Los pasos 1, 2 y 3 se pueden hacer ya. El resto depende de tener la cuenta de
desarrollador y el certificado.
