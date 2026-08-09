# Despliegue

Recetas se despliega en **agfserver-angel** (`192.168.0.185`), detrás del túnel
de Cloudflare `angelgf.com.es`.

```
Internet → Cloudflare → cloudflared → nginx :80 ─┬→ /apps/recetas/current/web  (estático)
                                                  └→ Kestrel :54009            (API)
```

| Pieza | Valor |
|---|---|
| Web | `https://recetas.angelgf.com.es` |
| API | `https://recetas-api.angelgf.com.es` |
| Puerto de Kestrel | `54009` (loopback; 54001–54008 estaban ocupados) |
| Usuario del servicio | `webapps` |
| Directorio base | `/apps/recetas` |
| Base de datos | `recetas`, autenticación `peer` por socket Unix |

## Despliegue habitual

```powershell
./deploy/publish.ps1
```

Compila API y web, genera el bundle de migraciones, transfiere y activa. Si algo
falla a partir de la parada del servicio, revierte a la release anterior.

Para revisar el paquete sin subir nada: `./deploy/publish.ps1 -SoloEmpaquetar`.

## Preparación del servidor (solo la primera vez)

Todos estos pasos necesitan `sudo`, que en este servidor **pide contraseña**, así
que hay que ejecutarlos a mano.

### 1. Base de datos

La autenticación es `peer` sobre el socket Unix: PostgreSQL identifica al cliente
por el usuario del sistema, así que el rol debe llamarse igual que el usuario que
ejecuta el servicio (`webapps`) y no hace falta contraseña.

```bash
sudo -u postgres psql <<'SQL'
-- El rol ya existe si hay otras aplicaciones en el servidor; no pasa nada si falla.
CREATE ROLE webapps WITH LOGIN;
CREATE DATABASE recetas OWNER webapps;
SQL
```

### 2. Directorios

```bash
sudo install -d -o webapps -g webapps -m 2775 /apps/recetas
sudo install -d -o webapps -g webapps -m 2775 /apps/recetas/releases /apps/recetas/incoming /apps/recetas/efbundle-cache
# Fotos de las recetas (feature 004). Se crea ya para no volver aquí.
sudo install -d -o webapps -g webapps -m 2775 /apps/recetas/fotos
```

El bit `2775` (setgid + escritura de grupo) es lo que permite que `angel`
despliegue y que `webapps` lea, sin pelearse por los permisos en cada release.

### 3. Configuración

Dos ficheros separados **por sensibilidad**: el script de despliegue necesita la
cadena de conexión para migrar, pero no debe poder leer los secretos.

```bash
sudo install -d -o root -g root -m 755 /etc/recetas
```

**`/etc/recetas/db.env`** — sin secretos, legible:

```bash
sudo tee /etc/recetas/db.env >/dev/null <<'EOF'
# Cadena de conexion. Sin contrasenia: autenticacion peer sobre el socket Unix.
# No contiene secretos, y por eso es legible: el script de despliegue la lee para
# aplicar las migraciones sin necesitar acceso a los secretos de la aplicacion.
ConnectionStrings__Recetas=Host=/var/run/postgresql;Username=webapps;Database=recetas
EOF
sudo chmod 644 /etc/recetas/db.env
```

**`/etc/recetas/api.env`** — secretos, solo para `webapps`:

```bash
# Genera una clave de firma aleatoria (la API no arranca con menos de 32 caracteres).
CLAVE_JWT="$(openssl rand -base64 48)"

sudo tee /etc/recetas/api.env >/dev/null <<EOF
Jwt__ClaveDeFirma=$CLAVE_JWT
Jwt__Emisor=recetas
Jwt__Audiencia=recetas

Correo__UsarBrevo=true
Correo__ClaveDeApi=PON_AQUI_LA_CLAVE_DE_BREVO
Correo__CorreoRemitente=no-responder@angelgf.com.es
Correo__NombreRemitente=Recetas
Correo__BaseDeLaWeb=https://recetas.angelgf.com.es

Moderacion__CorreoDelResponsable=angelgf@gmail.com

Cors__Origenes__0=https://recetas.angelgf.com.es
EOF

sudo chown webapps:webapps /etc/recetas/api.env
sudo chmod 600 /etc/recetas/api.env
```

> **`Moderacion__CorreoDelResponsable` tiene que ser el correo de una cuenta real
> de la aplicación.** Es quien recibe las denuncias y el único que puede retirar
> recetas publicadas ajenas, y se reconoce comparando ese valor con el correo del
> token. Si no coincide con ninguna cuenta, las denuncias llegan igual pero nadie
> puede actuar sobre ellas. La API arranca sin este valor: el fallo no se nota
> hasta que hay algo que moderar.

> **Cambiar `Jwt__ClaveDeFirma` invalida todas las sesiones abiertas.** No es
> grave —los usuarios vuelven a iniciar sesión—, pero conviene saberlo antes de
> regenerarla sin querer.

> Las variables de entorno de .NET usan `__` (dos guiones bajos) donde la
> configuración usa `:`. Y los arrays se indexan: `Cors__Origenes__0`.

### 4. nginx

```bash
scp deploy/nginx/recetas agfserver-angel:/tmp/recetas-nginx
ssh -t agfserver-angel "sudo install -o root -g root -m 644 /tmp/recetas-nginx /etc/nginx/sites-available/recetas \
  && sudo ln -sfn /etc/nginx/sites-available/recetas /etc/nginx/sites-enabled/recetas \
  && sudo nginx -t && sudo systemctl reload nginx"
```

Encadenado con `&&` a propósito: si `nginx -t` falla no llega a recargar, y el
servidor sigue sirviendo la configuración anterior.

### 5. systemd

```bash
scp deploy/systemd/recetas-api.service agfserver-angel:/tmp/recetas-api.service
ssh -t agfserver-angel "sudo install -o root -g root -m 644 /tmp/recetas-api.service /etc/systemd/system/recetas-api.service \
  && sudo systemctl daemon-reload \
  && sudo systemctl enable recetas-api"
```

**No se arranca todavía**: sin una release desplegada, `/apps/recetas/current` no
existe y el servicio entraría en bucle de reinicio. El primer
`./deploy/publish.ps1` lo arranca.

### 6. Permitir que el despliegue gestione el servicio

`remote-activate.sh` para y arranca el servicio con `sudo -n` (sin contraseña).
Sin esta regla, cada despliegue se quedaría colgado pidiéndola.

```bash
sudo tee /etc/sudoers.d/recetas-deploy >/dev/null <<'EOF'
angel ALL=(root) NOPASSWD: /usr/bin/systemctl start recetas-api, /usr/bin/systemctl stop recetas-api
angel ALL=(webapps) NOPASSWD: SETENV: /apps/recetas/releases/*/efbundle
EOF
sudo chmod 440 /etc/sudoers.d/recetas-deploy
sudo visudo -c
```

Acotada a propósito: solo arrancar y parar *ese* servicio, y ejecutar el bundle
de migraciones *como* `webapps`. No es un permiso general de administración.

> **`SETENV:` no es opcional.** El script invoca el bundle con
> `sudo --preserve-env=ConnectionStrings__Recetas,DOTNET_BUNDLE_EXTRACT_BASE_DIR`,
> y sudo rechaza conservar el entorno si la regla no lleva esa etiqueta. Sin ella
> el despliegue falla en las migraciones con un mensaje de autenticación que no
> apunta en absoluto a la causa.

Si el despliegue falla con `sudo: interactive authentication is required` o
`A terminal is required to authenticate`, es que este fichero falta o su regla no
casa con el comando: comprueba que `which systemctl` devuelve `/usr/bin/systemctl`.

### 7. Correo (Brevo)

Sin esto el alta de usuarios **se rompe en silencio**: los mensajes salen pero
acaban en spam, y el usuario nunca recibe el enlace.

- Autenticar el dominio remitente en Brevo: añadir los registros **SPF y DKIM**
  que indique su panel al DNS de `angelgf.com.es` en Cloudflare.
- Verificar el remitente `no-responder@angelgf.com.es`.
- Comprobar el límite diario del plan contratado: agotarlo deja el registro
  inutilizable.

### 8. Copia de seguridad: son DOS piezas

El volcado de PostgreSQL **no incluye las fotos**: los binarios viven en
`/apps/recetas/fotos` y la base de datos solo guarda la referencia. Una copia que
cubra solo la base de datos restauraría las recetas con las imágenes rotas.

**La base de datos ya está cubierta** por `/opt/scripts/pgbackup.sh`, que recorre
todas las bases del servidor y rota siete días.

**Las fotos las cubre `backup-ficheros.sh`**, en esta misma carpeta. Instalarlo:

```bash
sudo cp deploy/backup-ficheros.sh /opt/scripts/
sudo chmod +x /opt/scripts/backup-ficheros.sh
```

Y en el cron de root, media hora después del de PostgreSQL para no solaparlos:

```
30 2 * * * /opt/scripts/backup-ficheros.sh >> /var/log/backup-ficheros.log 2>&1
```

Sigue la convención de `pgbackup.sh` —un archivo por día de la semana, rotación
de siete— y deja las copias en `/var/backups/ficheros`, con permisos `700` en el
directorio y `600` en los archivos: llevan las fotos de los usuarios dentro.

Hace tres cosas que el de PostgreSQL no hace, y que conviene mantener:

- **Escribe en un `.parcial` y renombra al terminar.** Si el proceso muere a
  medias, queda un archivo con nombre evidente y **no se ha destruido la copia
  buena** de la semana anterior.
- **Comprueba que el archivo se puede leer** (`tar -tzf`) antes de darlo por
  bueno. Un disco lleno produce un `.tar.gz` corrupto que si no, nadie descubre
  hasta el día que hace falta.
- **Comprueba el espacio libre** antes de empezar. Una copia que llena el disco
  tumba la aplicación que pretendía proteger.

Que `tar` devuelva `1` no es un fallo: significa que un archivo cambió mientras
se leía, normal con la aplicación en marcha. Se registra como aviso.

#### Fuera del servidor

Una copia en el mismo disco que los datos cubre un borrado accidental, no que la
máquina se muera. Las dos piezas se sincronizan con OneDrive por `rclone`, cada
una en su línea del cron de root:

```
37 2 * * * rclone sync /var/backups/postgresql onedrive:/Backups/postgresql --config /home/angel/.config/rclone/rclone.conf --log-file=/var/log/rclone_backup_2.log
45 2 * * * rclone sync /var/backups/ficheros   onedrive:/Backups/ficheros   --config /home/angel/.config/rclone/rclone.conf --log-file=/var/log/rclone_backup_ficheros.log
```

Tres detalles que no son casuales:

- **`--config` explícito.** El cron corre como root, y `rclone` buscaría su
  configuración en `/root/.config`, donde no está: la cuenta está autorizada en
  el perfil de `angel`. Sin esa opción, `rclone` arranca con los valores por
  defecto y no encuentra ningún remoto.
- **Las 2:45.** La copia de ficheros termina a las 2:30 y la sincronización de
  PostgreSQL arranca a las 2:37. Dos `rclone` simultáneos contra la misma cuenta
  pueden estorbarse al refrescar el testigo de acceso.
- **`sync`, no `copy`.** Refleja el origen, así que la rotación de siete días se
  mantiene también en OneDrive en vez de acumular archivos para siempre.

  **El precio de `sync` es que borra en destino lo que no esté en origen.** Si un
  día la copia local fallara y dejara el directorio vacío, la sincronización se
  llevaría por delante la copia remota. Por eso `backup-ficheros.sh` descarta los
  archivos incompletos en lugar de dejarlos a medias, y no toca la copia del día
  anterior hasta tener la nueva verificada.

> **Lo que sube a OneDrive son fotos de las casas de los usuarios.** Va sin
> cifrar, en una cuenta personal. Si algún día eso deja de ser suficiente,
> `rclone` tiene un remoto `crypt` que cifra en origen y no cambia nada más del
> procedimiento.

#### Restaurar

```bash
sudo -u webapps psql recetas < Copia_recetas_N.sql
sudo tar -xzf Copia_ficheros_recetas_N.tar.gz -C /apps/recetas
sudo chown -R webapps:webapps /apps/recetas/fotos
```

**Probado el 7 de agosto de 2026**: copia de las 14 fotos de producción,
restaurada en un directorio limpio y comparada con `diff -r` contra el original.
Idénticas, miniaturas incluidas.

**Y el 8 de agosto, el ciclo completo pasando por OneDrive:**

```bash
sudo rclone copy onedrive:/Backups/ficheros/Copia_ficheros_recetas_6.tar.gz /tmp/desde-onedrive/ \
    --config /home/angel/.config/rclone/rclone.conf
sudo rm -rf /tmp/verifica && sudo mkdir -p /tmp/verifica
sudo tar -xzf /tmp/desde-onedrive/Copia_ficheros_recetas_6.tar.gz -C /tmp/verifica
sudo diff -r /apps/recetas/fotos /tmp/verifica/fotos && echo "IDENTICAS"
sudo rm -rf /tmp/desde-onedrive /tmp/verifica
```

Una copia que no se ha restaurado nunca no es una copia, y una copia remota que
nunca se ha bajado tampoco. **Conviene repetir esta comprobación de vez en
cuando**: lo que se estropea en silencio no suele ser el script, sino el testigo
de acceso a OneDrive, que caduca sin avisar a nadie.

## La IP real del cliente

Cloudflare → cloudflared → nginx → Kestrel: **todos los saltos son locales**, así
que sin cabeceras de reenvío cada petición llegaría a la API como `127.0.0.1` y
el limitador metería a todos los usuarios en el mismo cubo. Bastaría un visitante
activo para dejar a los demás sin alta y sin inicio de sesión.

Lo resuelven dos piezas que hay que mantener juntas: nginx **sobrescribe**
`X-Forwarded-For` con `$http_cf_connecting_ip` —no lo acumula, o un cliente
podría inyectar entradas falsas— y la API confía en un único salto, solo desde
loopback.

**Comprobado el 9 de agosto de 2026 en producción.** La prueba, por si hay que
repetirla tras tocar nginx o el túnel:

```bash
# 1. Agotar el límite desde fuera: 10 permitidos, el 11 debe dar 429.
for i in $(seq 1 11); do
  curl -s -o /dev/null -w "%{http_code}\n" -X POST https://recetas-api.angelgf.com.es/sesiones \
    -H 'Content-Type: application/json' \
    -d '{"correo":"sonda@ejemplo.com","contrasena":"credencial-falsa-1234"}'
done

# 2. Y acto seguido, desde el servidor y directo a Kestrel:
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://127.0.0.1:54009/sesiones \
  -H 'Content-Type: application/json' \
  -d '{"correo":"sonda@ejemplo.com","contrasena":"credencial-falsa-1234"}'
```

**`401` en el segundo paso significa que funciona**: los cubos son distintos. Un
`429` significaría que el tráfico externo se está identificando como local, es
decir, que las cabeceras no llegan.

> **El registro de nginx no guarda la IP real.** Usa el formato `combined`, cuyo
> `$remote_addr` es el túnel, así que **todo aparece como `127.0.0.1`**. El
> limitador sí distingue, pero si algún día hay abuso el registro no dirá de
> dónde viene. Se arregla con un `log_format` que incluya
> `$http_cf_connecting_ip`, y afecta a todas las aplicaciones del servidor.

## Vigilancia

`GET /salud` responde `200` solo si **la base de datos responde y el disco de las
fotos acepta escrituras con espacio suficiente**; si algo falla, `503` y el cuerpo
dice qué pieza. Está pensado para una sonda externa: apunta un monitor
—UptimeRobot, Better Stack o equivalente— a
`https://recetas-api.angelgf.com.es/salud` cada pocos minutos.

**Externo, no en el servidor.** Un vigilante que vive en la máquina que vigila se
cae con ella, que es justo cuando hace falta.

El umbral de espacio libre es `Fotos:MinimoDeEspacioLibreEnMb`, 200 MB por
defecto. Ese disco lo comparten ahora las fotos y las copias de seguridad de
todas las aplicaciones del servidor.

## Comprobación

```bash
ssh agfserver-angel "systemctl status recetas-api --no-pager | head -20"
ssh agfserver-angel "curl -fsS http://127.0.0.1:54009/salud"
curl -fsS https://recetas-api.angelgf.com.es/salud
```

`/salud` responde `200` con `{"estado":"correcto","baseDeDatos":true}` cuando la
API alcanza PostgreSQL, y `503` cuando no.

## Reversión

Las 5 últimas releases se conservan en `/apps/recetas/releases`.

```bash
ssh -t agfserver-angel "sudo systemctl stop recetas-api \
  && ln -sfn /apps/recetas/releases/<RELEASE-ANTERIOR> /apps/recetas/current \
  && sudo systemctl start recetas-api"
```

> **Las migraciones no se revierten solas.** Si la release fallida cambió el
> esquema, volver al código anterior no deshace ese cambio. Mientras las
> migraciones sean aditivas no hay problema; una que borre o renombre columnas
> exige un plan de vuelta atrás propio.

## Registros

```bash
ssh agfserver-angel "journalctl -u recetas-api -n 100 --no-pager"
ssh agfserver-angel "sudo tail -f /var/log/nginx/recetas-api.error.log"
```
