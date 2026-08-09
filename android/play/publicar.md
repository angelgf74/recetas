# Cómo publicar Recetas en Google Play

Lista de lo que falta, **en orden de dependencia**. Cada bloque dice quién puede
hacerlo: hay cosas que son código y otras que exigen decisiones o una cuenta.

> Nada de esto es asesoramiento legal. Los apartados marcados con ⚖ conviene que
> los revise alguien que sepa, sobre todo si Recetas deja de ser un proyecto
> personal.

---

## 1. ~~Mensaje de consentimiento del RGPD en AdMob~~ · HECHO

El mensaje está **creado y publicado**. Comprobado en el móvil: el formulario sale
en español con las tres opciones (*Consentir*, *No consentir*, *Gestionar
opciones*), y el registro pasó de `no form(s) configured` a
`Consentimiento consultado`.

> **Queda una decisión tuya:** el formulario dice que los datos podrán ser
> consultados por **210 partners**. Es la selección amplia por defecto. Reducirla
> baja los ingresos y baja también el número de empresas con datos de tus
> usuarios. Se cambia en el mismo sitio (**Privacidad y mensajes → Reglamentos
> europeos**) sin tocar código, y **hay que volver a publicar** el mensaje.

Lo que sigue se conserva por si hay que rehacerlo.

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

## 5. ~~Firmar la aplicación~~ · HECHO

El almacén está en `C:/Desarrollo/Proyectos/keystores/recetas-release.jks`, las
credenciales en `android/keystore.properties` (fuera de git) y el paquete
firmado se genera con `gradlew bundleRelease`.

**Comprobado el 6 de agosto de 2026** instalando la compilación de publicación
en un teléfono real: iniciar sesión, recetario con miniaturas, ficha con foto,
escalar raciones y buscar. Todo correcto.

Esa prueba encontró un fallo que la compilación no daba: R8 borraba la base de
datos Room de WorkManager —que llega con `play-services-ads`— y la aplicación
moría al arrancar, sin haber emitido una sola advertencia al compilar. Está
arreglado en `proguard-rules.pro`. **Repetir la prueba después de tocar
dependencias o reglas de ProGuard.**

Lo que sigue se conserva por si hay que rehacer el almacén.

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

**Configurar Gradle.** El `signingConfig` ya está cableado: solo falta crear
`android/keystore.properties` (ignorado por git) con estas cuatro líneas.

```
storeFile=C:/ruta/segura/recetas-release.jks
storePassword=…
keyAlias=recetas
keyPassword=…
```

Las barras van hacia delante (`/`) incluso en Windows: en un archivo de
propiedades de Java, `\` es un carácter de escape y una ruta con `\r` o `\n`
dentro se lee mal.

**No mandes las contraseñas por el chat**: quedarían escritas en el historial de
la conversación. Ponlas tú en el archivo.

**Generar el AAB:**

```powershell
.\gradlew.bat bundleRelease
```

Sale en `app/build/outputs/bundle/release/app-release.aab`. Sin
`keystore.properties` el paquete se genera igual pero **sin firmar**, y avisa al
empaquetar.

**Comprobar que va firmado** antes de subirlo:

```powershell
& "$env:ProgramFiles\Android\Android Studio\jbr\bin\jarsigner.exe" -verify `
    app\build\outputs\bundle\release\app-release.aab
```

`jar verified` es lo que hay que ver. `no manifest` significa que salió sin
firma.

### Probar el paquete de publicación antes de subirlo · IMPORTANTE

La compilación de publicación pasa por **R8**, que borra el código que cree que
no se usa. `kotlinx.serialization` genera sus serializadores por reflexión, así
que R8 no ve que se usan: hay reglas en `proguard-rules.pro` para protegerlos,
pero **que compile no demuestra que funcionen**. El fallo típico aparece en
ejecución, al leer la primera respuesta de la API, y no se reproduce en
depuración.

Instálalo y ábrelo antes de subir nada:

```powershell
.\gradlew.bat installRelease
```

Comprueba al menos: iniciar sesión, ver el recetario, abrir una receta y buscar.
Si algo falla ahí, es R8, no el servidor.

> **Ojo con los anuncios en esta prueba:** la compilación de publicación usa los
> bloques **reales**. Míralos si quieres, pero **no los pulses**: pulsar tus
> propios anuncios es tráfico inválido y AdMob suspende cuentas por ello.

---

## 6. ~~Añadir la huella de publicación a assetlinks.json~~ · HECHO Y COMPROBADO

Los enlaces del correo del alta y de recuperar contraseña abren la aplicación
gracias a `/.well-known/assetlinks.json`. **La huella que va ahí es la del
certificado con el que Play refirma** (`4A:7E:85:…`), junto a la de depuración;
la de subida **no sirve** para esto.

**Comprobado el 9 de agosto de 2026 con la aplicación instalada desde la tienda:**

```powershell
adb shell pm get-app-links com.angelgf.recetas
```

```
Signatures: [4A:7E:85:0B:...]          <- la de Play, no la de subida
recetas.angelgf.com.es: verified
```

Con la copia instalada por cable —firmada con la clave de subida— ese mismo
comando decía `1024`, verificación fallida. **Es la forma de distinguir los dos
casos**, porque el síntoma de que esto no funcione es silencioso: el enlace
simplemente abre el navegador, sin ningún error.

Lo que sigue se conserva por si hay que rehacerlo.

**Por qué:** la aplicación descargada de Play tiene otra firma que la compilada
en casa, y con la huella equivocada **los enlaces dejan de abrirla**.

**La huella del certificado con el que se firma hoy** ya está sacada. Se lee del
propio paquete, sin necesidad de la contraseña del almacén:

```powershell
& "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe" `
    -printcert -jarfile app\build\outputs\bundle\release\app-release.aab
```

```
73:F6:37:06:F1:C3:3A:6B:33:DD:67:6B:BB:D4:07:EA:95:EE:94:70:40:2F:0E:49:B3:67:76:C3:2D:76:32:8F
```

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
sexo, sin drogas, sin juego, sin compras.

**La que no es "no" es la de contenido generado por usuarios.** La pregunta suena
así: *"¿La aplicación permite de forma nativa que los usuarios interactúen o
intercambien contenido con otros usuarios?"* La respuesta es **sí**: una receta
publicada la ven los demás usuarios al buscar, con su texto y sus fotos.
Responder "no" sería falso y se comprueba en un minuto con la cuenta de
demostración.

Esa respuesta activa la política de contenido generado por usuarios, que exige
poder **denunciar** y poder **actuar**. Las dos cosas existen desde la feature
015: hay un enlace para denunciar en la ficha de cualquier receta ajena —web y
Android—, la denuncia avisa por correo al responsable, y este puede retirarla de
la parte pública desde la propia ficha. Retirar la devuelve a privada; no borra
nada del autor.

---

## 9. Seguridad de los datos

**Dónde:** Play Console → **Contenido de la aplicación** → **Seguridad de los
datos**.

Con lo que la aplicación hace de verdad, las respuestas son:

| Categoría de Play | Recogido | Compartido | Obligatorio | Para qué |
| --- | --- | --- | --- | --- |
| Información personal → **Correo electrónico** | Sí | No | **Sí** | Gestión de la cuenta |
| Fotos y vídeos → **Fotos** | Sí | No | No | Funciones de la aplicación |
| **Otros datos** (recetas: nombre, ingredientes, pasos) | Sí | No | No | Funciones de la aplicación |
| ID de dispositivo → **ID de publicidad** | Sí | **Sí** | No | Publicidad o marketing |
| Ubicación, contactos, agenda, micrófono | No | — | — | — |

| Pregunta transversal | Respuesta |
| --- | --- |
| ¿Se cifran los datos en tránsito? | **Sí**, todo va por HTTPS |
| ¿Puede el usuario pedir que se eliminen? | **Sí** |
| ¿Hay forma de eliminar la cuenta? | **Sí**, desde la aplicación y desde la web (feature 016) |
| URL de eliminación | `https://recetas.angelgf.com.es/borrar-cuenta.html` |

### Tres cosas que se responden mal con facilidad

**"Compartido" no significa "lo ve un tercero".** Google excluye a los
proveedores que procesan por cuenta del desarrollador. Brevo ve el correo para
poder entregarlo y Cloudflare ve la conexión, pero ambos son proveedores de
servicio y **no se declaran como compartidos**. AdMob sí: usa el identificador
para su propio negocio publicitario.

**Casi nada es obligatorio.** Solo el correo. Las fotos y las recetas las pone el
usuario si quiere, y el identificador publicitario **se puede rechazar** desde el
formulario de consentimiento sin perder ninguna función.

**La dirección IP de los registros del servidor es el caso dudoso.** La política
la menciona. Google exime los datos de procesamiento efímero, pero los registros
de `journald` persisten, así que "efímero" es discutible. La lectura conservadora
—no declararla— se sostiene en que no la recoge la aplicación: la recibe el
servidor por el hecho de la conexión y solo sirve para limitar abusos. Si se
quiere declarar, la categoría es *Actividad de la aplicación → Otras acciones*.

**Todo esto está respaldado por la política de privacidad publicada.** Si cambias
una respuesta aquí, cambia también la política.

La política ya describe la publicidad tal y como funciona hoy: qué trata AdMob,
que la base legal es tu consentimiento, que implica transferencia fuera del EEE y
que se revoca desde **Ajustes → Opciones de privacidad de los anuncios**.

---

## 10. Declarar que contiene anuncios

**Dónde:** Play Console → **Contenido de la aplicación** → **Anuncios**.

Responde **sí**. Aunque hoy solo salgan anuncios de prueba, la versión publicada
llevará reales. Declarar que no y luego mostrarlos es una infracción.

---

## Orden recomendado

Los pasos 1, 2 y 3 están hechos. Queda:

1. Cuenta de desarrollador de Play (4), si no la tienes ya.
2. Almacén de claves y AAB (5), y con eso la huella (6).
3. Todo lo demás de Play Console (7, 8, 9, 10).

De estos, el único que puede traer sorpresa es el **8**: hay contenido generado
por usuarios y no existe mecanismo de denuncia.
