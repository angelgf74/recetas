<#
.SYNOPSIS
    Carga las recetas de ejemplo en un recetario, a través de la API.

.DESCRIPTION
    Inicia sesión con las credenciales que se le pasen y crea cada receta del
    fichero recetas-de-ejemplo.json.

    Va por la API y no por SQL a propósito: así pasa por las mismas validaciones
    que cualquier receta creada a mano, y si algún dato del fichero es inválido se
    entera aquí y no al abrir la aplicación.

    Es idempotente por omisión: antes de crear, comprueba si ya existe una receta
    con ese nombre y la salta. Repetir el comando no duplica el recetario.

.PARAMETER Api
    Base de la API. Por defecto la local.

.PARAMETER Correo
    Correo de la cuenta donde se cargan las recetas.

.PARAMETER Publicar
    Publica cada receta después de crearla.

.EXAMPLE
    ./datos/cargar-recetas.ps1 -Correo yo@ejemplo.com

.EXAMPLE
    ./datos/cargar-recetas.ps1 -Api https://recetas-api.angelgf.com.es -Correo yo@ejemplo.com -Publicar
#>
[CmdletBinding()]
param(
    [string]$Api = 'http://localhost:5199',
    [Parameter(Mandatory = $true)][string]$Correo,
    [switch]$Publicar
)

$ErrorActionPreference = 'Stop'

function Paso($texto) { Write-Host "==> $texto" -ForegroundColor Cyan }
function Detalle($texto) { Write-Host "     $texto" }

$Api = $Api.TrimEnd('/')
$fichero = Join-Path $PSScriptRoot 'recetas-de-ejemplo.json'

if (-not (Test-Path $fichero)) {
    throw "No se encuentra $fichero."
}

$recetas = Get-Content $fichero -Raw -Encoding UTF8 | ConvertFrom-Json
Paso "$($recetas.Count) recetas en el fichero"

# ---------------------------------------------------------------------------
# Credenciales
# ---------------------------------------------------------------------------
#
# Se pide por consola con Read-Host -AsSecureString para que la contraseña no
# quede en el historial del terminal ni en la línea de comandos, que es visible
# para cualquier proceso de la máquina.

$segura = Read-Host -Prompt "Contraseña de $Correo" -AsSecureString
$credencial = New-Object System.Management.Automation.PSCredential($Correo, $segura)
$contrasena = $credencial.GetNetworkCredential().Password

Paso 'Iniciando sesión'

try {
    $sesion = Invoke-RestMethod -Uri "$Api/sesiones" -Method Post -ContentType 'application/json' `
        -Body (@{ correo = $Correo; contrasena = $contrasena } | ConvertTo-Json)
}
catch {
    throw "No se ha podido iniciar sesión: $($_.Exception.Message)"
}
finally {
    $contrasena = $null
}

$cabeceras = @{ Authorization = "Bearer $($sesion.token)" }
Detalle 'Sesión iniciada'

# ---------------------------------------------------------------------------
# Recetas que ya existen
# ---------------------------------------------------------------------------

$existentes = @{}

foreach ($receta in (Invoke-RestMethod -Uri "$Api/recetas" -Headers $cabeceras)) {
    $existentes[$receta.nombre] = $receta.id
}

Detalle "$($existentes.Count) recetas ya en el recetario"

# ---------------------------------------------------------------------------
# Carga
# ---------------------------------------------------------------------------

$creadas = 0
$saltadas = 0
$fallidas = 0

foreach ($receta in $recetas) {
    if ($existentes.ContainsKey($receta.nombre)) {
        $saltadas++
        continue
    }

    $cuerpo = @{
        nombre       = $receta.nombre
        tipoDePlato  = $receta.tipoDePlato
        elaboracion  = $receta.elaboracion
        ingredientes = @($receta.ingredientes | ForEach-Object {
            @{ nombre = $_.nombre; cantidad = $_.cantidad; unidad = $_.unidad }
        })
    } | ConvertTo-Json -Depth 6

    try {
        $creada = Invoke-RestMethod -Uri "$Api/recetas" -Method Post -Headers $cabeceras `
            -ContentType 'application/json; charset=utf-8' `
            -Body ([System.Text.Encoding]::UTF8.GetBytes($cuerpo))

        if ($Publicar) {
            Invoke-RestMethod -Uri "$Api/recetas/$($creada.id)/publicacion" -Method Post -Headers $cabeceras | Out-Null
        }

        $creadas++
        Detalle "$($receta.tipoDePlato.PadRight(15)) $($receta.nombre)"
    }
    catch {
        $fallidas++
        Write-Warning "Falló '$($receta.nombre)': $($_.Exception.Message)"
    }
}

Paso 'Resumen'
Detalle "Creadas:  $creadas"
Detalle "Saltadas: $saltadas (ya existían)"

if ($fallidas -gt 0) {
    Detalle "Fallidas: $fallidas"
    exit 1
}
