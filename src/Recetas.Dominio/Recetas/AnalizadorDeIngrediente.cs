using System.Globalization;
using System.Text.RegularExpressions;

namespace Recetas.Dominio.Recetas;

/// <summary>
/// Parte una línea de ingrediente escrita a mano —"300 g de harina"— en cantidad,
/// unidad y nombre.
/// </summary>
/// <remarks>
/// Es una heurística, no un analizador completo: las webs escriben esto de mil
/// maneras y no hay forma de acertar siempre. Por eso la regla que gobierna el
/// tipo es <b>no perder nada</b>: si algo no encaja, el texto entero pasa a ser el
/// nombre del ingrediente y la línea se queda sin cantidad. El usuario revisa el
/// formulario antes de guardar, así que una lectura pobre es una molestia; una
/// línea desaparecida sería una receta mal copiada sin avisar.
/// </remarks>
public static partial class AnalizadorDeIngrediente
{
    /// <summary>Tope del nombre, el mismo que admite <see cref="NombreDeIngrediente"/>.</summary>
    private const int LongitudMaximaDelNombre = 80;

    /// <summary>
    /// Cantidad al principio de la línea, seguida opcionalmente de una unidad.
    /// </summary>
    /// <remarks>
    /// Admite decimales con coma o punto ("1,5"), fracciones ("1/2") y rangos
    /// ("2-3"), de los que se toma el primer número: para la lista de la compra es
    /// más útil quedarse corto que largo.
    /// </remarks>
    [GeneratedRegex(
        @"^\s*(?<cantidad>\d+(?:[.,]\d+)?(?:\s*/\s*\d+)?)(?:\s*[-–a]\s*\d+(?:[.,]\d+)?)?\s*(?<resto>.*)$",
        RegexOptions.ExplicitCapture)]
    private static partial Regex ConCantidad { get; }

    /// <summary>
    /// Fracciones de un solo carácter. Las webs de recetas las usan mucho —"½
    /// cucharadita"— y sin traducirlas la línea entera se daría por ilegible.
    /// </summary>
    private static readonly Dictionary<char, string> FraccionesDeUnCaracter = new()
    {
        ['¼'] = "1/4",
        ['½'] = "1/2",
        ['¾'] = "3/4",
        ['⅐'] = "1/7",
        ['⅑'] = "1/9",
        ['⅒'] = "1/10",
        ['⅓'] = "1/3",
        ['⅔'] = "2/3",
        ['⅕'] = "1/5",
        ['⅖'] = "2/5",
        ['⅗'] = "3/5",
        ['⅘'] = "4/5",
        ['⅙'] = "1/6",
        ['⅚'] = "5/6",
        ['⅛'] = "1/8",
        ['⅜'] = "3/8",
        ['⅝'] = "5/8",
        ['⅞'] = "7/8"
    };

    /// <summary>Texto entre paréntesis: "harina (de fuerza)" estorba más que aporta.</summary>
    [GeneratedRegex(@"\s*\([^)]*\)")]
    private static partial Regex Parentesis { get; }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosSeguidos { get; }

    /// <summary>
    /// Unidades reconocidas, con las abreviaturas y plurales que aparecen en las
    /// webs en español. La clave se compara ya en minúsculas y sin puntos.
    /// </summary>
    private static readonly Dictionary<string, Unidad> Unidades = new(StringComparer.Ordinal)
    {
        ["g"] = Unidad.Gramo,
        ["gr"] = Unidad.Gramo,
        ["gramo"] = Unidad.Gramo,
        ["gramos"] = Unidad.Gramo,
        ["kg"] = Unidad.Kilogramo,
        ["kilo"] = Unidad.Kilogramo,
        ["kilos"] = Unidad.Kilogramo,
        ["kilogramo"] = Unidad.Kilogramo,
        ["kilogramos"] = Unidad.Kilogramo,
        ["ml"] = Unidad.Mililitro,
        ["mililitro"] = Unidad.Mililitro,
        ["mililitros"] = Unidad.Mililitro,
        ["cl"] = Unidad.Mililitro,
        ["l"] = Unidad.Litro,
        ["litro"] = Unidad.Litro,
        ["litros"] = Unidad.Litro,
        ["cucharada"] = Unidad.Cucharada,
        ["cucharadas"] = Unidad.Cucharada,
        ["cda"] = Unidad.Cucharada,
        ["cdas"] = Unidad.Cucharada,
        ["cucharadita"] = Unidad.Cucharadita,
        ["cucharaditas"] = Unidad.Cucharadita,
        ["cdta"] = Unidad.Cucharadita,
        ["cdtas"] = Unidad.Cucharadita,
        ["taza"] = Unidad.Taza,
        ["tazas"] = Unidad.Taza,
        ["pizca"] = Unidad.Pizca,
        ["pizcas"] = Unidad.Pizca,
        ["diente"] = Unidad.Diente,
        ["dientes"] = Unidad.Diente,
        ["rama"] = Unidad.Rama,
        ["ramas"] = Unidad.Rama,
        ["ramita"] = Unidad.Rama,
        ["ramitas"] = Unidad.Rama,
        ["hoja"] = Unidad.Hoja,
        ["hojas"] = Unidad.Hoja,
        ["unidad"] = Unidad.Unidad,
        ["unidades"] = Unidad.Unidad,
        ["ud"] = Unidad.Unidad,
        ["uds"] = Unidad.Unidad,

        // Inglesas, solo las que tienen equivalente exacto en la lista cerrada.
        // Buena parte de las webs de recetas están en inglés, y sin esto la unidad
        // se queda dentro del nombre del ingrediente.
        //
        // No se traducen onzas ni libras: convertir de peso imperial a métrico es
        // otro problema, y está fuera del alcance de la 011.
        ["cup"] = Unidad.Taza,
        ["cups"] = Unidad.Taza,
        ["tablespoon"] = Unidad.Cucharada,
        ["tablespoons"] = Unidad.Cucharada,
        ["tbsp"] = Unidad.Cucharada,
        ["teaspoon"] = Unidad.Cucharadita,
        ["teaspoons"] = Unidad.Cucharadita,
        ["tsp"] = Unidad.Cucharadita,
        ["clove"] = Unidad.Diente,
        ["cloves"] = Unidad.Diente,
        ["pinch"] = Unidad.Pizca,
        ["sprig"] = Unidad.Rama,
        ["sprigs"] = Unidad.Rama,
        ["leaf"] = Unidad.Hoja,
        ["leaves"] = Unidad.Hoja
    };

    /// <summary>
    /// Palabras que sobran entre la unidad y el ingrediente: "300 g <b>de</b> harina".
    /// </summary>
    private static readonly string[] Enlaces = ["de", "del", "de la", "de los", "de las"];

    /// <summary>
    /// Interpreta la línea. Devuelve <c>null</c> solo si no queda ningún texto
    /// aprovechable, que es el único caso en el que no hay nada que conservar.
    /// </summary>
    public static LineaDeIngredienteImportada? Analizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var limpio = Normalizar(texto);

        if (limpio.Length == 0)
        {
            return null;
        }

        var coincidencia = ConCantidad.Match(limpio);

        if (!coincidencia.Success
            || !TryLeerCantidad(coincidencia.Groups["cantidad"].Value, out var cantidad))
        {
            // Sin número al principio: "sal al gusto", "aceite de oliva". Se guarda
            // entero y sin cantidad, que es exactamente lo que dice la línea.
            return new LineaDeIngredienteImportada(Recortar(limpio), null, Unidad.AlGusto);
        }

        var resto = coincidencia.Groups["resto"].Value.Trim();
        var unidad = Unidad.Unidad;

        if (TrySepararUnidad(resto, out var unidadLeida, out var sinUnidad))
        {
            unidad = unidadLeida;
            resto = sinUnidad;
        }

        resto = QuitarEnlaceInicial(resto);

        if (resto.Length == 0)
        {
            // Línea degenerada tipo "2 unidades": hay número y unidad pero ningún
            // ingrediente. Se conserva el texto entero, que es lo único que había.
            return new LineaDeIngredienteImportada(Recortar(limpio), null, Unidad.AlGusto);
        }

        // Pizca no lleva número en el dominio, y "al gusto" tampoco.
        if (unidad == Unidad.Pizca)
        {
            cantidad = Math.Max(1m, Math.Round(cantidad));
        }

        return new LineaDeIngredienteImportada(Recortar(resto), cantidad, unidad);
    }

    private static string Normalizar(string texto)
    {
        var sinParentesis = Parentesis.Replace(texto, string.Empty);
        var sinFracciones = ExpandirFracciones(sinParentesis);

        return EspaciosSeguidos.Replace(sinFracciones, " ").Trim();
    }

    /// <summary>Convierte "½" en "1/2" para que el resto del análisis lo entienda.</summary>
    private static string ExpandirFracciones(string texto)
    {
        if (!texto.Any(FraccionesDeUnCaracter.ContainsKey))
        {
            return texto;
        }

        var construido = new System.Text.StringBuilder(texto.Length + 8);

        foreach (var caracter in texto)
        {
            if (FraccionesDeUnCaracter.TryGetValue(caracter, out var fraccion))
            {
                construido.Append(fraccion);
            }
            else
            {
                construido.Append(caracter);
            }
        }

        return construido.ToString();
    }

    /// <summary>Corta al máximo que admite el dominio en vez de rechazar la línea.</summary>
    private static string Recortar(string texto) =>
        texto.Length <= LongitudMaximaDelNombre
            ? texto
            : texto[..LongitudMaximaDelNombre].TrimEnd();

    private static bool TryLeerCantidad(string texto, out decimal cantidad)
    {
        cantidad = 0m;

        var limpio = texto.Replace(",", ".").Replace(" ", string.Empty);

        // Fracciones: "1/2 cucharadita".
        var barra = limpio.IndexOf('/');

        if (barra > 0)
        {
            var numerador = limpio[..barra];
            var denominador = limpio[(barra + 1)..];

            if (decimal.TryParse(numerador, NumberStyles.Number, CultureInfo.InvariantCulture, out var arriba)
                && decimal.TryParse(denominador, NumberStyles.Number, CultureInfo.InvariantCulture, out var abajo)
                && abajo != 0)
            {
                cantidad = arriba / abajo;
                return cantidad > 0;
            }

            return false;
        }

        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out cantidad)
            && cantidad > 0;
    }

    private static bool TrySepararUnidad(string resto, out Unidad unidad, out string sinUnidad)
    {
        unidad = Unidad.Unidad;
        sinUnidad = resto;

        var espacio = resto.IndexOf(' ');
        var primera = espacio < 0 ? resto : resto[..espacio];

        // "gr." y "ml." llevan punto en muchas webs.
        var clave = primera.TrimEnd('.').ToLowerInvariant();

        if (!Unidades.TryGetValue(clave, out unidad))
        {
            return false;
        }

        sinUnidad = espacio < 0 ? string.Empty : resto[(espacio + 1)..].Trim();
        return true;
    }

    private static string QuitarEnlaceInicial(string texto)
    {
        foreach (var enlace in Enlaces.OrderByDescending(valor => valor.Length))
        {
            if (texto.StartsWith(enlace + " ", StringComparison.OrdinalIgnoreCase))
            {
                return texto[(enlace.Length + 1)..].Trim();
            }
        }

        return texto;
    }
}
