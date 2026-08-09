using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <summary>
/// Saca una receta del marcado <c>schema.org/Recipe</c> incrustado en la página.
/// </summary>
/// <remarks>
/// Solo JSON-LD. Es lo que publica prácticamente toda web de recetas, porque es lo
/// que pide Google para mostrar la ficha enriquecida; cubrir además microdatos y
/// RDFa multiplicaría este código para una minoría de sitios.
/// <para>
/// Cada web escribe su JSON-LD a su manera —el mismo campo aparece como texto, como
/// lista o como objeto—, así que todo se lee de forma defensiva: lo que no encaja
/// se ignora en lugar de tirar la importación entera.
/// </para>
/// </remarks>
public static partial class LectorDeRecetaEnJsonLd
{
    [GeneratedRegex(
        """<script[^>]*type\s*=\s*["']application/ld\+json["'][^>]*>(?<json>.*?)</script>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex BloquesJsonLd { get; }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Etiquetas { get; }

    [GeneratedRegex(@"\d+")]
    private static partial Regex PrimerNumero { get; }

    /// <summary>
    /// Devuelve el borrador, o <c>null</c> si la página no publica ninguna receta
    /// reconocible.
    /// </summary>
    public static RecetaImportada? Leer(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        foreach (Match bloque in BloquesJsonLd.Matches(html))
        {
            var receta = BuscarReceta(bloque.Groups["json"].Value);

            if (receta is not null)
            {
                return receta;
            }
        }

        return null;
    }

    private static RecetaImportada? BuscarReceta(string json)
    {
        JsonDocument documento;

        try
        {
            documento = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            // Un bloque mal formado no debe impedir mirar los demás: muchas webs
            // llevan varios, y solo uno es la receta.
            return null;
        }

        using (documento)
        {
            return BuscarEn(documento.RootElement, profundidad: 0);
        }
    }

    /// <summary>
    /// Busca el objeto de tipo <c>Recipe</c>. Puede venir suelto, dentro de una
    /// lista o colgando de un <c>@graph</c>, según la web.
    /// </summary>
    private static RecetaImportada? BuscarEn(JsonElement elemento, int profundidad)
    {
        // Tope de anidamiento: la entrada la controla un tercero y no conviene
        // recorrer un documento profundo a capricho.
        if (profundidad > 6)
        {
            return null;
        }

        switch (elemento.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var hijo in elemento.EnumerateArray())
                {
                    var encontrada = BuscarEn(hijo, profundidad + 1);

                    if (encontrada is not null)
                    {
                        return encontrada;
                    }
                }

                return null;

            case JsonValueKind.Object:
                if (EsReceta(elemento))
                {
                    return Mapear(elemento);
                }

                if (elemento.TryGetProperty("@graph", out var grafo))
                {
                    return BuscarEn(grafo, profundidad + 1);
                }

                return null;

            default:
                return null;
        }
    }

    /// <summary>El tipo puede ser texto o lista: "Recipe" o ["Recipe", "NewsArticle"].</summary>
    private static bool EsReceta(JsonElement objeto)
    {
        if (!objeto.TryGetProperty("@type", out var tipo))
        {
            return false;
        }

        return tipo.ValueKind switch
        {
            JsonValueKind.String => EsRecipe(tipo.GetString()),
            JsonValueKind.Array => tipo.EnumerateArray()
                .Any(valor => valor.ValueKind == JsonValueKind.String && EsRecipe(valor.GetString())),
            _ => false
        };
    }

    private static bool EsRecipe(string? tipo) =>
        string.Equals(tipo, "Recipe", StringComparison.OrdinalIgnoreCase);

    private static RecetaImportada? Mapear(JsonElement receta)
    {
        var nombre = TextoDe(receta, "name");

        // Sin nombre no hay receta que importar: es el único campo imprescindible.
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        var ingredientes = LeerIngredientes(receta);
        var elaboracion = LeerElaboracion(receta);

        return new RecetaImportada(
            Recortar(nombre, Receta.LongitudMaximaDelNombre),
            Recortar(elaboracion, Receta.LongitudMaximaDeLaElaboracion),
            LeerRaciones(receta),
            ingredientes,
            LeerImagen(receta));
    }

    /// <summary>
    /// La foto viene en varias formas según <c>schema.org</c>: texto suelto, lista
    /// de textos, un objeto <c>ImageObject</c> con <c>url</c>, o una lista de esos
    /// objetos. Se queda con la primera que parezca una dirección; validar que es
    /// http(s) de verdad es cosa de quien la descarga (027).
    /// </summary>
    private static string? LeerImagen(JsonElement receta)
    {
        if (!receta.TryGetProperty("image", out var imagen))
        {
            return null;
        }

        return PrimeraUrlDeImagen(imagen, profundidad: 0);
    }

    private static string? PrimeraUrlDeImagen(JsonElement elemento, int profundidad)
    {
        if (profundidad > 3)
        {
            return null;
        }

        switch (elemento.ValueKind)
        {
            case JsonValueKind.String:
                var texto = elemento.GetString();
                return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

            case JsonValueKind.Array:
                foreach (var hijo in elemento.EnumerateArray())
                {
                    var encontrada = PrimeraUrlDeImagen(hijo, profundidad + 1);

                    if (encontrada is not null)
                    {
                        return encontrada;
                    }
                }

                return null;

            // ImageObject: { "@type": "ImageObject", "url": "..." }
            case JsonValueKind.Object:
                return TextoDe(elemento, "url");

            default:
                return null;
        }
    }

    private static IReadOnlyList<LineaDeIngredienteImportada> LeerIngredientes(JsonElement receta)
    {
        if (!receta.TryGetProperty("recipeIngredient", out var lista))
        {
            // Algunas webs antiguas aún usan el nombre viejo del campo.
            receta.TryGetProperty("ingredients", out lista);
        }

        var lineas = new List<LineaDeIngredienteImportada>();

        foreach (var texto in TextosDe(lista))
        {
            var linea = AnalizadorDeIngrediente.Analizar(texto);

            if (linea is not null)
            {
                lineas.Add(linea);
            }
        }

        return lineas;
    }

    /// <summary>
    /// La elaboración llega como texto suelto, como lista de textos o como lista de
    /// objetos <c>HowToStep</c>, y a veces con etiquetas HTML dentro.
    /// </summary>
    private static string LeerElaboracion(JsonElement receta)
    {
        if (!receta.TryGetProperty("recipeInstructions", out var instrucciones))
        {
            return string.Empty;
        }

        var pasos = new List<string>();
        RecogerPasos(instrucciones, pasos, profundidad: 0);

        return string.Join("\n", pasos);
    }

    private static void RecogerPasos(JsonElement elemento, List<string> pasos, int profundidad)
    {
        if (profundidad > 4)
        {
            return;
        }

        switch (elemento.ValueKind)
        {
            case JsonValueKind.String:
                AnadirPaso(elemento.GetString(), pasos);
                break;

            case JsonValueKind.Array:
                foreach (var hijo in elemento.EnumerateArray())
                {
                    RecogerPasos(hijo, pasos, profundidad + 1);
                }

                break;

            case JsonValueKind.Object:
                // HowToSection agrupa pasos dentro de itemListElement.
                if (elemento.TryGetProperty("itemListElement", out var elementos))
                {
                    RecogerPasos(elementos, pasos, profundidad + 1);
                    return;
                }

                AnadirPaso(TextoDe(elemento, "text") ?? TextoDe(elemento, "name"), pasos);
                break;
        }
    }

    private static void AnadirPaso(string? texto, List<string> pasos)
    {
        var limpio = SinEtiquetas(texto);

        if (limpio.Length > 0)
        {
            pasos.Add(limpio);
        }
    }

    /// <summary>
    /// Las raciones vienen como número, como texto ("4 raciones") o como lista.
    /// Fuera del rango admitido se descartan: es preferible no decir nada a colocar
    /// un dato del que después depende el escalado de la feature 010.
    /// </summary>
    private static int? LeerRaciones(JsonElement receta)
    {
        if (!receta.TryGetProperty("recipeYield", out var valor))
        {
            return null;
        }

        var texto = valor.ValueKind switch
        {
            JsonValueKind.Number => valor.ToString(),
            JsonValueKind.String => valor.GetString(),
            JsonValueKind.Array => valor.EnumerateArray()
                .Select(hijo => hijo.ValueKind == JsonValueKind.String ? hijo.GetString() : hijo.ToString())
                .FirstOrDefault(hijo => !string.IsNullOrWhiteSpace(hijo)),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var numero = PrimerNumero.Match(texto);

        if (!numero.Success
            || !int.TryParse(numero.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raciones))
        {
            return null;
        }

        return raciones >= Receta.RacionesMinimas && raciones <= Receta.RacionesMaximas ? raciones : null;
    }

    private static string? TextoDe(JsonElement objeto, string propiedad)
    {
        if (!objeto.TryGetProperty(propiedad, out var valor))
        {
            return null;
        }

        return valor.ValueKind switch
        {
            JsonValueKind.String => valor.GetString(),
            JsonValueKind.Number => valor.ToString(),

            // "name": ["Tortilla"] aparece más de lo que debería.
            JsonValueKind.Array => valor.EnumerateArray()
                .FirstOrDefault(hijo => hijo.ValueKind == JsonValueKind.String)
                .GetStringOrNull(),
            _ => null
        };
    }

    private static IEnumerable<string> TextosDe(JsonElement elemento)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.String:
                var texto = elemento.GetString();

                if (!string.IsNullOrWhiteSpace(texto))
                {
                    yield return SinEtiquetas(texto);
                }

                break;

            case JsonValueKind.Array:
                foreach (var hijo in elemento.EnumerateArray())
                {
                    if (hijo.ValueKind == JsonValueKind.String)
                    {
                        var valor = SinEtiquetas(hijo.GetString());

                        if (valor.Length > 0)
                        {
                            yield return valor;
                        }
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Quita etiquetas HTML y desescapa las entidades. No es sanear para pintar
    /// —eso lo hace Blazor al renderizar—, es que "&lt;p&gt;" dentro de un paso es
    /// ruido para quien lee la receta.
    /// </summary>
    private static string SinEtiquetas(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : System.Net.WebUtility.HtmlDecode(Etiquetas.Replace(texto, " ")).Trim();

    private static string Recortar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..maximo].TrimEnd();
}

file static class ExtensionesDeJson
{
    /// <summary>
    /// <c>FirstOrDefault</c> sobre <c>JsonElement</c> devuelve un elemento
    /// <c>Undefined</c>, no <c>null</c>: pedirle el texto directamente reventaría.
    /// </summary>
    public static string? GetStringOrNull(this JsonElement elemento) =>
        elemento.ValueKind == JsonValueKind.String ? elemento.GetString() : null;
}
