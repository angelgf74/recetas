#!/usr/bin/env bash
# ==============================================================================
# backup-ficheros.sh
# Copia de seguridad de los directorios de archivos de las aplicaciones.
#
# Complementa a /opt/scripts/pgbackup.sh, que copia las bases de datos.
# LAS DOS PIEZAS SON NECESARIAS: el volcado de PostgreSQL guarda la fila que
# describe una foto, pero no la foto. Restaurar solo la base de datos deja un
# recetario con todas las imágenes rotas.
#
# Misma convención que pgbackup.sh: un archivo por directorio y día de la
# semana, con rotación de 7 días.
#
#   Copia_ficheros_recetas_1.tar.gz   ... _7.tar.gz
#
# Instalación:
#   sudo cp backup-ficheros.sh /opt/scripts/
#   sudo chmod +x /opt/scripts/backup-ficheros.sh
#
# En el cron de root, media hora después del de PostgreSQL para no solaparlos:
#   30 2 * * * /opt/scripts/backup-ficheros.sh >> /var/log/backup-ficheros.log 2>&1
# ==============================================================================

set -uo pipefail

# ------------------------------------------------------------------------------
# CONFIGURACIÓN
# ------------------------------------------------------------------------------

# Qué se copia: "nombre:ruta", separados por espacios. El nombre va en el archivo
# resultante, así que conviene que diga de qué aplicación es.
DIRECTORIOS=(
    "recetas:/apps/recetas/fotos"
)

# Se puede sobrescribir por entorno para probar el script sin tocar las copias
# de verdad ni necesitar root:
#   BACKUP_DIR=/tmp/prueba ./backup-ficheros.sh
BACKUP_DIR="${BACKUP_DIR:-/var/backups/ficheros}"
LOG_PREFIX="[BACKUP-FICHEROS]"

# Margen de disco exigido antes de empezar, en megas. Una copia que llena el
# disco no solo falla: tumba la aplicación que intentaba proteger.
MARGEN_LIBRE_MB=500

# ------------------------------------------------------------------------------
# VARIABLES INTERNAS
# ------------------------------------------------------------------------------

# 1=lunes … 7=domingo, igual que en pgbackup.sh.
DOW=$(date +%u)
ERRORES=0

log() {
    echo "$LOG_PREFIX [$(date +'%Y-%m-%d %H:%M:%S')] $1"
}

log_error() {
    echo "$LOG_PREFIX [ERROR] [$(date +'%Y-%m-%d %H:%M:%S')] $1" >&2
    ERRORES=$((ERRORES + 1))
}

# ------------------------------------------------------------------------------
# INICIO
# ------------------------------------------------------------------------------

log "======================================================"
log "Iniciando copia de ficheros — Día de semana: $DOW"
log "======================================================"

if [[ ! -d "$BACKUP_DIR" ]]; then
    mkdir -p "$BACKUP_DIR" || { log_error "No se pudo crear $BACKUP_DIR"; exit 1; }
    log "Directorio creado: $BACKUP_DIR"
fi

# Solo root y quien administre: estas copias llevan las fotos de los usuarios.
chmod 700 "$BACKUP_DIR"

for ENTRADA in "${DIRECTORIOS[@]}"; do
    NOMBRE="${ENTRADA%%:*}"
    RUTA="${ENTRADA#*:}"

    log "  Procesando: '$NOMBRE' → $RUTA"

    if [[ ! -d "$RUTA" ]]; then
        log_error "    No existe el directorio $RUTA"
        continue
    fi

    NECESARIO_MB=$(du -sm "$RUTA" 2>/dev/null | cut -f1)
    LIBRE_MB=$(df -Pm "$BACKUP_DIR" | awk 'NR==2 {print $4}')

    # Se compara contra el tamaño sin comprimir: es el peor caso, y las fotos
    # ya vienen comprimidas, así que gzip apenas las va a reducir.
    if (( LIBRE_MB < NECESARIO_MB + MARGEN_LIBRE_MB )); then
        log_error "    Espacio insuficiente: ${LIBRE_MB} MB libres, ${NECESARIO_MB} MB de datos más ${MARGEN_LIBRE_MB} MB de margen."
        continue
    fi

    ARCHIVO="Copia_ficheros_${NOMBRE}_${DOW}.tar.gz"
    DESTINO="$BACKUP_DIR/$ARCHIVO"
    TEMPORAL="$DESTINO.parcial"

    # Se escribe en un temporal y se renombra al final. Si el proceso muere a
    # medias, lo que queda es un `.parcial` evidente y NO se ha destruido la
    # copia buena de la semana pasada, que es lo que pasaría escribiendo
    # directamente sobre el destino.
    rm -f "$TEMPORAL"

    tar -czf "$TEMPORAL" -C "$(dirname "$RUTA")" "$(basename "$RUTA")"
    ESTADO=$?

    # tar devuelve 1 cuando un archivo cambió mientras lo leía. Con la
    # aplicación en marcha eso es normal —alguien acaba de subir una foto— y no
    # invalida la copia: solo significa que ese archivo puede haber quedado a
    # medias. El 2 sí es un fallo de verdad.
    if [[ $ESTADO -eq 1 ]]; then
        log "    Aviso: algún archivo cambió durante la copia (la aplicación estaba en uso)."
    elif [[ $ESTADO -ne 0 ]]; then
        log_error "    Falló la copia de '$NOMBRE' (tar devolvió $ESTADO)"
        rm -f "$TEMPORAL"
        continue
    fi

    # Comprobación de que el archivo se puede leer entero. Sin esto, un disco
    # lleno o un fallo de escritura produce un .tar.gz corrupto que nadie
    # descubre hasta el día que hace falta restaurarlo.
    if ! tar -tzf "$TEMPORAL" > /dev/null 2>&1; then
        log_error "    El archivo generado no se puede leer. Se descarta."
        rm -f "$TEMPORAL"
        continue
    fi

    ARCHIVOS=$(tar -tzf "$TEMPORAL" | wc -l)

    mv -f "$TEMPORAL" "$DESTINO"
    chmod 600 "$DESTINO"

    TAMANO=$(du -sh "$DESTINO" | cut -f1)
    log "    OK — $TAMANO, $ARCHIVOS elementos"
done

# ------------------------------------------------------------------------------
# RESUMEN
# ------------------------------------------------------------------------------

log "======================================================"
if [[ $ERRORES -eq 0 ]]; then
    log "Copia de ficheros completada sin errores"
else
    log_error "Copia de ficheros completada con $ERRORES error(es)"
fi
log "======================================================"

exit $ERRORES
