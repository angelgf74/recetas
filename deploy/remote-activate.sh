#!/usr/bin/env bash
#
# Activa una release ya transferida a agfserver-angel.
#
# Lo invoca deploy/publish.ps1 por SSH; viaja dentro del propio paquete, de modo
# que la versión desplegada y el procedimiento que la activa son siempre la
# misma revisión del repositorio.
#
# Secuencia: extraer, parar el servicio, migrar, cambiar el enlace, arrancar y
# comprobar salud. Si algo falla a partir de la parada, vuelve a la release
# anterior. Se acepta el corte de unos segundos: no hay requisito de despliegue
# sin interrupción.

set -Eeuo pipefail

RELEASE="${1:-}"
BASE="${2:-}"
APP_USER="${3:-}"
[ -n "$RELEASE" ] && [ -n "$BASE" ] && [ -n "$APP_USER" ] || {
    echo "Uso: remote-activate.sh <release> <directorio-base> <usuario-aplicacion>" >&2
    exit 2
}
RELEASES="$BASE/releases"
INCOMING="$BASE/incoming"
CURRENT="$BASE/current"
TARGET="$RELEASES/$RELEASE"

# Solo la cadena de conexión, que con autenticación `peer` no contiene secretos.
# Los secretos de la aplicación (clave de firma JWT, clave de API de Brevo) viven
# en api.env con permisos 600, y este script no necesita leerlos.
DB_ENV_FILE=/etc/recetas/db.env
HEALTH_URL="http://127.0.0.1:54009/salud"
HEALTH_TIMEOUT=45
KEEP=5
SERVICES="recetas-api"

log()  { printf '     %s\n' "$*"; }
fail() { printf '     ERROR: %s\n' "$*" >&2; exit 1; }

PREVIOUS=""
SERVICES_STOPPED=0

rollback() {
    if [ "$SERVICES_STOPPED" -eq 0 ]; then
        return
    fi
    if [ -n "$PREVIOUS" ] && [ -d "$PREVIOUS" ]; then
        log "Revirtiendo a $(basename "$PREVIOUS")"
        ln -sfn "$PREVIOUS" "$CURRENT"
        # shellcheck disable=SC2086
        sudo systemctl start $SERVICES || log "No se ha podido arrancar el servicio; revisa journalctl."
    else
        log "No hay release anterior a la que volver: el servicio queda parado."
    fi
}

# --------------------------------------------------------------------------
# Extracción
# --------------------------------------------------------------------------

[ -f "$INCOMING/$RELEASE.tar.gz" ] || fail "no se encuentra $INCOMING/$RELEASE.tar.gz"
[ -f "$DB_ENV_FILE" ] || fail "falta $DB_ENV_FILE (ver deploy/README.md)"
id -u "$APP_USER" >/dev/null 2>&1 || fail "el usuario $APP_USER no existe en el servidor"

log "Extrayendo $RELEASE"
rm -rf "$TARGET"
mkdir -p "$TARGET"
tar -xzf "$INCOMING/$RELEASE.tar.gz" -C "$TARGET"
chmod +x "$TARGET/efbundle"
rm -f "$INCOMING/$RELEASE.tar.gz" "$INCOMING/remote-activate.sh"

if [ -L "$CURRENT" ]; then
    PREVIOUS="$(readlink -f "$CURRENT")"
fi

# --------------------------------------------------------------------------
# Migraciones
# --------------------------------------------------------------------------
#
# Se migra con el servicio parado. Así se evita por completo que el código
# antiguo llegue a ver el esquema nuevo, que es la clase de fallo más difícil de
# diagnosticar después.

log "Deteniendo el servicio"
# shellcheck disable=SC2086
sudo systemctl stop $SERVICES || true
SERVICES_STOPPED=1

log "Leyendo configuración"

# NO se hace `. "$DB_ENV_FILE"`. La cadena de conexión contiene `;`, y al
# sourcear el fichero bash los interpreta como separadores de comandos:
# asignaría solo `Host=...` y ejecutaría el resto como órdenes sueltas.
#
# systemd no tiene este problema porque su parser de EnvironmentFile no es un
# shell, así que el fichero se deja sin comillas por y para él.
#
# Sourcear también ejecutaría cualquier cosa que hubiera en el fichero; extraer
# solo la clave que interesa evita ambas cosas.
conn="$(sed -n 's/^[[:space:]]*ConnectionStrings__Recetas[[:space:]]*=//p' "$DB_ENV_FILE" | head -n 1)"

# Por si alguien la entrecomilla a mano en el futuro.
conn="${conn%\"}"; conn="${conn#\"}"
conn="${conn%\'}"; conn="${conn#\'}"

if [ -z "$conn" ]; then
    rollback
    fail "$DB_ENV_FILE no define ConnectionStrings__Recetas"
fi

# Comprobación de integridad: si el valor llega troceado, aquí se ve. Sin ella,
# el fallo aparece mucho más adelante y disfrazado.
case "$conn" in
    *Database=*) : ;;
    *)
        rollback
        fail "la cadena de conexión no incluye Database=; ha llegado incompleta: '$conn'" ;;
esac

export ConnectionStrings__Recetas="$conn"

# Las migraciones deben ejecutarse como el usuario de la aplicación: PostgreSQL
# usa autenticación `peer` sobre el socket Unix, así que identifica al cliente
# por el usuario del sistema. Ejecutarlas como el usuario de despliegue las haría
# fallar con un error de autenticación desconcertante, porque la cadena de
# conexión sería correcta.
log "Aplicando migraciones como $APP_USER"

# El bundle es un ejecutable de fichero único y se autoextrae antes de arrancar.
# Sin esta variable usaría el $HOME de webapps, que no existe y está fuera de
# /apps. Aquí se mantiene todo dentro del árbol de la aplicación.
export DOTNET_BUNDLE_EXTRACT_BASE_DIR="$BASE/efbundle-cache"
mkdir -p "$DOTNET_BUNDLE_EXTRACT_BASE_DIR"
chmod g+w "$DOTNET_BUNDLE_EXTRACT_BASE_DIR" 2>/dev/null || true

# --preserve-env es imprescindible: sudo limpia el entorno. La cadena de conexión
# la exige la composición de dependencias al construir el host, antes de que
# --connection llegue a aplicarse.
if [ "$(id -un)" = "$APP_USER" ]; then
    migrate() { "$TARGET/efbundle" --connection "$1"; }
else
    migrate() {
        sudo -n -u "$APP_USER" \
            --preserve-env=ConnectionStrings__Recetas,DOTNET_BUNDLE_EXTRACT_BASE_DIR \
            "$TARGET/efbundle" --connection "$1"
    }
fi

if ! migrate "$ConnectionStrings__Recetas"; then
    rollback
    fail "fallo al migrar; no se ha activado la release nueva"
fi

# --------------------------------------------------------------------------
# Activación
# --------------------------------------------------------------------------

log "Activando $RELEASE"
ln -sfn "$TARGET" "$CURRENT"

log "Arrancando el servicio"
# shellcheck disable=SC2086
if ! sudo systemctl start $SERVICES; then
    rollback
    fail "el servicio no ha arrancado; revisa journalctl -u recetas-api"
fi

# --------------------------------------------------------------------------
# Comprobación de salud
# --------------------------------------------------------------------------
#
# Contra 127.0.0.1, no contra el dominio público: aquí solo se decide si la
# aplicación vive. Meter a Cloudflare en esta decisión provocaría reversiones por
# un problema de red ajeno al despliegue.
#
# /salud responde 503 si PostgreSQL no contesta, y `curl -f` trata el 503 como
# fallo. Es lo que se quiere: una API que no alcanza su base de datos no está
# lista para recibir tráfico.

log "Comprobando salud (hasta ${HEALTH_TIMEOUT}s)"
healthy=0
for _ in $(seq 1 "$HEALTH_TIMEOUT"); do
    if curl -fsS --max-time 3 "$HEALTH_URL" >/dev/null 2>&1; then
        healthy=1
        break
    fi
    sleep 1
done

if [ "$healthy" -ne 1 ]; then
    log "La aplicación no responde en $HEALTH_URL"
    # shellcheck disable=SC2086
    sudo systemctl stop $SERVICES || true
    rollback
    fail "despliegue revertido por fallo de la comprobación de salud"
fi

log "Salud correcta"

# --------------------------------------------------------------------------
# Limpieza
# --------------------------------------------------------------------------
#
# Se conservan las últimas $KEEP releases para poder revertir a mano. La actual
# se excluye del borrado de forma explícita: si por lo que sea no estuviera entre
# las más recientes, borrarla dejaría el enlace `current` apuntando a la nada.

log "Conservando las últimas $KEEP releases"
current_target="$(readlink -f "$CURRENT" 2>/dev/null || true)"

# shellcheck disable=SC2012
ls -1dt "$RELEASES"/*/ 2>/dev/null | tail -n +$((KEEP + 1)) | while read -r vieja; do
    vieja="${vieja%/}"
    if [ "$(readlink -f "$vieja")" = "$current_target" ]; then
        continue
    fi
    log "Eliminando $(basename "$vieja")"
    rm -rf "$vieja"
done

log "Despliegue completado: $RELEASE"
