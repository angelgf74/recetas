using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// Genera los recursos gráficos de la ficha de Google Play a partir de la misma
// figura del icono de la aplicación (la olla), para que la tienda, el lanzador y
// la web no parezcan tres productos distintos.

var salida = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(salida);

var acento = Color.ParseHex("B4541F");
var claro = Color.ParseHex("FDFCFA");
var fondoSuave = Color.ParseHex("FDF4EE");

// Rectángulo con esquinas redondeadas. ImageSharp.Drawing no trae uno, y sin
// esquinas la olla queda con aristas duras que no se parecen al favicon.
static IPath RectRedondeado(float x, float y, float ancho, float alto, float radio)
{
    radio = MathF.Min(radio, MathF.Min(ancho, alto) / 2f);

    var constructor = new PathBuilder();

    constructor.MoveTo(new PointF(x + radio, y));
    constructor.AddLine(new PointF(x + ancho - radio, y), new PointF(x + ancho - radio, y));
    constructor.AddArc(new RectangleF(x + ancho - 2 * radio, y, 2 * radio, 2 * radio), 0, 270, 90);
    constructor.AddLine(new PointF(x + ancho, y + alto - radio), new PointF(x + ancho, y + alto - radio));
    constructor.AddArc(new RectangleF(x + ancho - 2 * radio, y + alto - 2 * radio, 2 * radio, 2 * radio), 0, 0, 90);
    constructor.AddLine(new PointF(x + radio, y + alto), new PointF(x + radio, y + alto));
    constructor.AddArc(new RectangleF(x, y + alto - 2 * radio, 2 * radio, 2 * radio), 0, 90, 90);
    constructor.AddLine(new PointF(x, y + radio), new PointF(x, y + radio));
    constructor.AddArc(new RectangleF(x, y, 2 * radio, 2 * radio), 0, 180, 90);
    constructor.CloseFigure();

    return constructor.Build();
}

// La olla, dibujada sobre un lienzo de 64 como en favicon.svg, escalada al vuelo.
// Las medidas y los radios son los mismos que los del SVG.
static IPath[] Olla(float escala, float dx, float dy)
{
    var piezas = new List<IPath>
    {
        new EllipsePolygon(32, 14, 4).AsClosedPath(),          // pomo
        RectRedondeado(11, 19, 42, 7, 3.5f),                    // tapa
        RectRedondeado(6, 31, 9, 7, 3.5f),                      // asa izquierda
        RectRedondeado(49, 31, 9, 7, 3.5f),                     // asa derecha
        RectRedondeado(14, 29, 36, 23, 6f)                      // cuerpo
    };

    return piezas
        .Select(p => p.Transform(Matrix3x2Extensions.CreateScale(new SizeF(escala, escala)))
                      .Transform(Matrix3x2Extensions.CreateTranslation(new PointF(dx, dy))))
        .ToArray();
}

// ------------------------------------------------------ Icono 512x512

using (var icono = new Image<Rgba32>(512, 512))
{
    icono.Mutate(c => c.BackgroundColor(acento));

    // 512 / 64 = 8. La figura ocupa el lienzo entero, como el favicon.
    foreach (var pieza in Olla(8f, 0, 0))
    {
        icono.Mutate(c => c.Fill(claro, pieza));
    }

    icono.Save(System.IO.Path.Combine(salida, "icono-512.png"));
    Console.WriteLine("icono-512.png");
}

// ------------------------------------- Gráfico destacado 1024x500

using (var destacado = new Image<Rgba32>(1024, 500))
{
    destacado.Mutate(c => c.BackgroundColor(fondoSuave));

    // Banda de acento a la izquierda con la olla dentro: deja el lado derecho
    // libre para el texto, que es donde Play recorta menos en pantallas anchas.
    destacado.Mutate(c => c.Fill(acento, new RectangularPolygon(0, 0, 340, 500)));

    // 500 * 0.62 / 64 ≈ 4.84 → figura centrada en la banda.
    const float escala = 4.6f;
    var ancho = 64 * escala;
    foreach (var pieza in Olla(escala, (340 - ancho) / 2f, (500 - ancho) / 2f))
    {
        destacado.Mutate(c => c.Fill(claro, pieza));
    }

    var familia = BuscarFuente();

    if (familia is { } f)
    {
        var titulo = f.CreateFont(76, FontStyle.Bold);
        var lema = f.CreateFont(34, FontStyle.Regular);

        destacado.Mutate(c => c
            .DrawText("Recetas", titulo, Color.ParseHex("24201C"), new PointF(400, 178))
            .DrawText("Tus recetas, siempre a mano", lema, Color.ParseHex("6B625A"), new PointF(402, 272)));
    }
    else
    {
        Console.WriteLine("AVISO: sin fuente disponible, el destacado va sin texto.");
    }

    destacado.Save(System.IO.Path.Combine(salida, "destacado-1024x500.png"));
    Console.WriteLine("destacado-1024x500.png");
}

static FontFamily? BuscarFuente()
{
    foreach (var nombre in new[] { "Segoe UI", "Arial", "Liberation Sans", "DejaVu Sans" })
    {
        if (SystemFonts.TryGet(nombre, out var familia))
        {
            return familia;
        }
    }

    return SystemFonts.Families.FirstOrDefault();
}
