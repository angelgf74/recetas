<#
.SYNOPSIS
    Compila, empaqueta y despliega Recetas en agfserver-angel.

.DESCRIPTION
    Cadena completa: publicar API y web, generar el bundle de migraciones,
    empaquetar, transferir por SSH y activar con deploy/remote-activate.sh.

    El script de activación viaja DENTRO del paquete, de modo que la versión
    desplegada y el procedimiento que la activa son siempre la misma revisión.

    Requisitos previos en el servidor (solo la primera vez): ver deploy/README.md.

.PARAMETER Host
    Alias SSH del servidor. Por defecto agfserver-angel.

.PARAMETER SoloEmpaquetar
    Genera el paquete y se detiene, sin transferir ni activar. Útil para revisar
    qué se va a subir.

.EXAMPLE
    ./deploy/publish.ps1
#>
[CmdletBinding()]
param(
    [string]$ServidorSsh = 'agfserver-angel',
    [string]$DirectorioBase = '/apps/recetas',
    [string]$UsuarioAplicacion = 'webapps',
    [switch]$SoloEmpaquetar
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$salida = Join-Path $raiz 'artifacts/deploy'

function Paso($texto) { Write-Host "==> $texto" -ForegroundColor Cyan }
function Detalle($texto) { Write-Host "     $texto" }

# ---------------------------------------------------------------------------
# Identificador de la release
# ---------------------------------------------------------------------------
#
# Marca de tiempo + revisión corta de git: ordenable cronológicamente y
# rastreable hasta el commit exacto. Si no hay git, se marca como "local" para
# que quede claro en el servidor que esa release no es reproducible.

$marca = Get-Date -Format 'yyyyMMdd-HHmmss'
$revision = 'local'

if (Test-Path (Join-Path $raiz '.git')) {
    # --verify --quiet no escribe nada en stderr cuando no hay commits todavía.
    # Importa: redirigir la salida de error de un ejecutable nativo con 2>$null
    # hace que PowerShell 5.1 la envuelva en un ErrorRecord y, con
    # ErrorActionPreference = 'Stop', aborte el script por algo que no es un fallo.
    & git -C $raiz rev-parse --verify --quiet HEAD | Out-Null

    if ($LASTEXITCODE -eq 0) {
        $revision = (& git -C $raiz rev-parse --short HEAD).Trim()

        if (& git -C $raiz status --porcelain) {
            Write-Warning 'El árbol de trabajo tiene cambios sin confirmar: la release no será reproducible desde git.'
        }
    }
    else {
        Write-Warning 'El repositorio no tiene ningún commit: la release se marca como "local" y no será rastreable.'
    }
}

$release = "$marca-$revision"

Paso "Release $release"

# ---------------------------------------------------------------------------
# Limpieza del directorio de salida
# ---------------------------------------------------------------------------

if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }
$paquete = Join-Path $salida $release
New-Item -ItemType Directory -Path $paquete -Force | Out-Null

# ---------------------------------------------------------------------------
# Compilación
# ---------------------------------------------------------------------------

Paso 'Publicando la API'
dotnet publish (Join-Path $raiz 'src/Recetas.Api') `
    -c Release `
    -o (Join-Path $paquete 'api') `
    --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación de la API.' }

Paso 'Publicando la web'
# El publicado de Blazor WebAssembly deja los ficheros servibles en wwwroot/;
# es ese subdirectorio, y no la raíz de la publicación, lo que nginx debe servir.
$webTemporal = Join-Path $salida 'web-publish'
dotnet publish (Join-Path $raiz 'src/Recetas.Web') `
    -c Release `
    -o $webTemporal `
    --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación de la web.' }

Copy-Item (Join-Path $webTemporal 'wwwroot') (Join-Path $paquete 'web') -Recurse
Remove-Item $webTemporal -Recurse -Force

# ---------------------------------------------------------------------------
# Configuración de la web para producción
# ---------------------------------------------------------------------------
#
# La web es estática: la URL de la API no se puede inyectar por variable de
# entorno al arrancar, porque no hay arranque en servidor. Se reescribe aquí, en
# el paquete, dejando intacto el appsettings.json del repositorio (que apunta a
# localhost para desarrollo).

Paso 'Apuntando la web a la API de producción'
$configWeb = Join-Path $paquete 'web/appsettings.json'
$json = @{ Api = @{ Base = 'https://recetas-api.angelgf.com.es/' } } | ConvertTo-Json -Depth 5

# Sin BOM y de forma explícita: `Set-Content -Encoding utf8` lo añade en
# PowerShell 5.1, y un BOM al principio de un JSON servido por HTTP es basura
# antes del primer `{` que algunos analizadores rechazan.
[System.IO.File]::WriteAllText($configWeb, $json, [System.Text.UTF8Encoding]::new($false))
Detalle 'web/appsettings.json -> https://recetas-api.angelgf.com.es/'

# Las copias precomprimidas que genera Blazor corresponden al fichero anterior y
# ya no cuadran con el que se acaba de escribir. Si nginx llegara a servirlas
# (con gzip_static), la web recibiría la URL de la API de desarrollo.
Remove-Item "$configWeb.br", "$configWeb.gz" -ErrorAction SilentlyContinue

# ---------------------------------------------------------------------------
# Bundle de migraciones
# ---------------------------------------------------------------------------
#
# Un ejecutable autocontenido con todas las migraciones. Se prefiere a
# `dotnet ef database update` en el servidor porque allí no hay SDK ni
# herramientas de EF instaladas, solo el runtime.

Paso 'Generando el bundle de migraciones'
dotnet ef migrations bundle `
    --project (Join-Path $raiz 'src/Recetas.Infraestructura') `
    --startup-project (Join-Path $raiz 'src/Recetas.Api') `
    --configuration Release `
    --target-runtime linux-x64 `
    --self-contained `
    --output (Join-Path $paquete 'efbundle') `
    --force
if ($LASTEXITCODE -ne 0) { throw 'Falló la generación del bundle de migraciones.' }

# ---------------------------------------------------------------------------
# Script de activación
# ---------------------------------------------------------------------------

Copy-Item (Join-Path $PSScriptRoot 'remote-activate.sh') $paquete

# ---------------------------------------------------------------------------
# Empaquetado
# ---------------------------------------------------------------------------
#
# tar con -C sobre el directorio del paquete para que el archivo no contenga el
# nombre de la release como carpeta raíz: el servidor lo extrae directamente
# dentro de releases/<release>/.

Paso 'Empaquetando'
$tar = Join-Path $salida "$release.tar.gz"
tar -czf $tar -C $paquete .
if ($LASTEXITCODE -ne 0) { throw 'Falló el empaquetado.' }

$tamano = [math]::Round((Get-Item $tar).Length / 1MB, 1)
Detalle "$tar ($tamano MB)"

if ($SoloEmpaquetar) {
    Paso 'Solo empaquetar: no se transfiere ni se activa.'
    return
}

# ---------------------------------------------------------------------------
# Transferencia
# ---------------------------------------------------------------------------

Paso "Transfiriendo a $ServidorSsh"
ssh $ServidorSsh "mkdir -p '$DirectorioBase/incoming' '$DirectorioBase/releases'"
if ($LASTEXITCODE -ne 0) { throw "No se ha podido preparar $DirectorioBase en el servidor." }

scp $tar "${ServidorSsh}:$DirectorioBase/incoming/$release.tar.gz"
if ($LASTEXITCODE -ne 0) { throw 'Falló la transferencia del paquete.' }

scp (Join-Path $PSScriptRoot 'remote-activate.sh') "${ServidorSsh}:$DirectorioBase/incoming/remote-activate.sh"
if ($LASTEXITCODE -ne 0) { throw 'Falló la transferencia del script de activación.' }

# ---------------------------------------------------------------------------
# Activación
# ---------------------------------------------------------------------------

Paso 'Activando en el servidor'
ssh $ServidorSsh "chmod +x '$DirectorioBase/incoming/remote-activate.sh' && '$DirectorioBase/incoming/remote-activate.sh' '$release' '$DirectorioBase' '$UsuarioAplicacion'"
if ($LASTEXITCODE -ne 0) { throw 'Falló la activación en el servidor. Revisa la salida y journalctl -u recetas-api.' }

Paso "Desplegado: $release"
Detalle 'https://recetas.angelgf.com.es'
Detalle 'https://recetas-api.angelgf.com.es/salud'
