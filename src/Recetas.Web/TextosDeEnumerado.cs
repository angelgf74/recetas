namespace Recetas.Web;

/// <summary>
/// Traduce a texto legible los valores que la API devuelve como identificadores
/// de enumerado.
/// </summary>
/// <remarks>
/// La API los manda como texto (<c>PlatoPrincipal</c>) para no depender del orden
/// del enumerado. Mostrarlos tal cual sería feo, y meter el texto bonito en el
/// contrato mezclaría presentación con datos.
/// </remarks>
public static class TextosDeEnumerado
{
    public static string DeTipo(string tipo) => tipo switch
    {
        "Entrante" => "Entrante",
        "PrimerPlato" => "Primer plato",
        "PlatoPrincipal" => "Plato principal",
        "Guarnicion" => "Guarnición",
        "Postre" => "Postre",
        "Bebida" => "Bebida",
        "Salsa" => "Salsa",
        _ => tipo
    };

    public static string DeUnidad(string unidad, decimal? cantidad) => unidad switch
    {
        "AlGusto" => "al gusto",
        "Unidad" => cantidad == 1 ? "unidad" : "unidades",
        "Gramo" => "g",
        "Kilogramo" => "kg",
        "Mililitro" => "ml",
        "Litro" => "l",
        "Cucharada" => cantidad == 1 ? "cucharada" : "cucharadas",
        "Cucharadita" => cantidad == 1 ? "cucharadita" : "cucharaditas",
        "Taza" => cantidad == 1 ? "taza" : "tazas",
        "Pizca" => cantidad == 1 ? "pizca" : "pizcas",
        "Diente" => cantidad == 1 ? "diente" : "dientes",
        "Rama" => cantidad == 1 ? "rama" : "ramas",
        "Hoja" => cantidad == 1 ? "hoja" : "hojas",
        _ => unidad
    };

    /// <summary>Cantidad sin ceros de más: 1,5 en lugar de 1,500.</summary>
    public static string DeCantidad(decimal? cantidad) =>
        cantidad is null ? string.Empty : cantidad.Value.ToString("0.###");
}
