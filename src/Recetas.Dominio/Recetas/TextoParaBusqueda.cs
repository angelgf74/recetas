using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Recetas.Dominio.Recetas;

/// <summary>
/// Convierte un texto a la forma con la que se busca: minúsculas, sin acentos y
/// con los espacios colapsados.
/// </summary>
/// <remarks>
/// <para>
/// En español se escribe "jamon" y se espera encontrar "Jamón". Lo habitual en
/// PostgreSQL sería la extensión <c>unaccent</c>, pero instalarla exige
/// privilegios sobre la base de datos que el despliegue no tiene. Se resuelve
/// guardando junto al nombre real una columna ya normalizada y buscando contra
/// ella.
/// </para>
/// <para>
/// <b>El texto guardado y el de la consulta pasan por aquí, y solo por aquí.</b>
/// Si cada lado normalizara con reglas distintas, no casarían nunca y el fallo
/// sería silencioso: búsquedas que no encuentran nada sin ningún error.
/// </para>
/// <para>
/// Los acentos se quitan solo para buscar. El nombre canónico los conserva, igual
/// que decidió la 003 para los ingredientes: en español distinguen palabras y
/// "anís" no debe mostrarse como "anis".
/// </para>
/// </remarks>
public static partial class TextoParaBusqueda
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var colapsado = EspaciosSeguidos().Replace(texto.Trim(), " ");

        // FormD separa cada letra acentuada en letra + marca diacrítica, con lo
        // que las marcas se pueden descartar una a una: "jamón" pasa a "jamon".
        //
        // La ñ también se descompone, así que acaba como "n". Es deliberado: se
        // escribe "pina colada" o "jalapeno" mucho más a menudo de lo que se
        // teclea la ñ, y aquí la prioridad es encontrar. El coste es que "ano" y
        // "año" se confunden al buscar; en un recetario es un precio asumible, y
        // el nombre que se muestra conserva la ñ intacta.
        var descompuesto = colapsado.Normalize(NormalizationForm.FormD);
        var construido = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                construido.Append(caracter);
            }
        }

        return construido
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex EspaciosSeguidos();
}
