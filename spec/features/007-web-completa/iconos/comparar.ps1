# Compara los tres candidatos de icono a los tamaños en que se usa un favicon.
# No toca el repositorio: solo genera una hoja de contacto para decidir.

Add-Type -AssemblyName System.Drawing

$ACENTO = [System.Drawing.Color]::FromArgb(180, 84, 31)
$CREMA  = [System.Drawing.Color]::FromArgb(253, 252, 250)

function RectRedondo($g, $brocha, $x, $y, $w, $h, $r) {
    if ($r -le 0) { $g.FillRectangle($brocha, $x, $y, $w, $h); return }
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, 2*$r, 2*$r, 180, 90)
    $p.AddArc($x+$w-2*$r, $y, 2*$r, 2*$r, 270, 90)
    $p.AddArc($x+$w-2*$r, $y+$h-2*$r, 2*$r, 2*$r, 0, 90)
    $p.AddArc($x, $y+$h-2*$r, 2*$r, 2*$r, 90, 90)
    $p.CloseFigure()
    $g.FillPath($brocha, $p)
}

# --- A · Olla vista de frente -------------------------------------------------
function DibujarOlla($g, $e, $ac, $cr) {
    $g.FillEllipse($cr, (32-4)*$e, (14-4)*$e, 8*$e, 8*$e)
    RectRedondo $g $cr (11*$e) (19*$e) (42*$e) (7*$e) (3.5*$e)
    RectRedondo $g $cr (6*$e)  (31*$e) (9*$e)  (7*$e) (3.5*$e)
    RectRedondo $g $cr (49*$e) (31*$e) (9*$e)  (7*$e) (3.5*$e)
    RectRedondo $g $cr (14*$e) (29*$e) (36*$e) (23*$e) (6*$e)
}

# --- B · Libro de recetas abierto ---------------------------------------------
function DibujarLibro($g, $e, $ac, $cr) {
    RectRedondo $g $cr (8*$e) (15*$e) (48*$e) (35*$e) (5*$e)
    # Lomo: la franja que hace que se lea como libro y no como tarjeta.
    $g.FillRectangle($ac, 30*$e, 15*$e, 4*$e, 35*$e)
    # Renglones. Desaparecen a 16 px, y es lo esperado: el lomo aguanta solo.
    foreach ($y in 24, 31, 38) {
        RectRedondo $g $ac (13*$e) ($y*$e) (13*$e) (3*$e) (1.5*$e)
        RectRedondo $g $ac (38*$e) ($y*$e) (13*$e) (3*$e) (1.5*$e)
    }
}

# --- C · Cubiertos cruzados ---------------------------------------------------
function DibujarCubiertos($g, $e, $ac, $cr) {
    $estado = $g.Save()

    # Cuchillo: barra en diagonal con la hoja más ancha arriba.
    $g.TranslateTransform(32*$e, 32*$e)
    $g.RotateTransform(45)
    RectRedondo $g $cr (-3.5*$e) (-22*$e) (7*$e) (44*$e) (3.5*$e)
    RectRedondo $g $cr (-5*$e)   (-22*$e) (10*$e) (18*$e) (5*$e)
    $g.Restore($estado)

    $estado = $g.Save()
    # Tenedor: la otra diagonal, con dos ranuras que insinúan tres púas.
    $g.TranslateTransform(32*$e, 32*$e)
    $g.RotateTransform(-45)
    RectRedondo $g $cr (-3.5*$e) (-22*$e) (7*$e) (44*$e) (3.5*$e)
    RectRedondo $g $cr (-6*$e)   (-22*$e) (12*$e) (16*$e) (3*$e)
    # Las ranuras solo se ven de 32 px para arriba; por debajo queda una pala.
    $g.FillRectangle($ac, -2.2*$e, -22*$e, 1.6*$e, 11*$e)
    $g.FillRectangle($ac,  0.6*$e, -22*$e, 1.6*$e, 11*$e)
    $g.Restore($estado)
}

function Generar($dibujo, $lado) {
    $bmp = New-Object System.Drawing.Bitmap ([int]$lado), ([int]$lado)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $e = $lado / 64.0
    $ac = New-Object System.Drawing.SolidBrush $ACENTO
    $cr = New-Object System.Drawing.SolidBrush $CREMA
    RectRedondo $g $ac 0 0 $lado $lado (14*$e)
    & $dibujo $g $e $ac $cr
    $g.Dispose()
    return $bmp
}

# --- Hoja de contacto ---------------------------------------------------------

$tamanos = 16, 24, 32, 48, 64
$disenos = @(
    @{ Nombre = "A · Olla";              Fn = ${function:DibujarOlla} },
    @{ Nombre = "B · Libro de recetas";  Fn = ${function:DibujarLibro} },
    @{ Nombre = "C · Cubiertos";         Fn = ${function:DibujarCubiertos} }
)

$anchoFila = 60 + ($tamanos | ForEach-Object { $_ + 34 } | Measure-Object -Sum).Sum
$altoBloque = 190
$hoja = New-Object System.Drawing.Bitmap ([int]$anchoFila), ([int]($altoBloque * $disenos.Count))
$g = [System.Drawing.Graphics]::FromImage($hoja)
$g.SmoothingMode = 'AntiAlias'
$g.InterpolationMode = 'HighQualityBicubic'
$g.Clear([System.Drawing.Color]::White)

$fuenteTitulo = New-Object System.Drawing.Font("Segoe UI Semibold", 11)
$fuentePeq = New-Object System.Drawing.Font("Segoe UI", 8)
$oscuro = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(28,28,28))

$fila = 0
foreach ($d in $disenos) {
    $y0 = $fila * $altoBloque

    $g.DrawString($d.Nombre, $fuenteTitulo, [System.Drawing.Brushes]::Black, [single]18, [single]($y0 + 10))
    # Mitad inferior del bloque en oscuro, para ver el icono sobre tema oscuro.
    $g.FillRectangle($oscuro, 0, $y0 + 108, $anchoFila, 74)

    $x = 30
    foreach ($t in $tamanos) {
        $img = Generar $d.Fn $t
        $g.DrawImage($img, [int]$x, [int]($y0 + 55), [int]$t, [int]$t)
        $g.DrawImage($img, [int]$x, [int]($y0 + 125), [int]$t, [int]$t)
        $img.Dispose()
        $g.DrawString("$t", $fuentePeq, [System.Drawing.Brushes]::Gray, [single]$x, [single]($y0 + 38))
        $x += $t + 34
    }

    $fila++
}

$g.Dispose()
$salida = Join-Path $PSScriptRoot "comparacion-iconos.png"
$hoja.Save($salida, [System.Drawing.Imaging.ImageFormat]::Png)
$hoja.Dispose()
Write-Output $salida
