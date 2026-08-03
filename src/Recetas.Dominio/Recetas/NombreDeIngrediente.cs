using System.Text.RegularExpressions;

namespace Recetas.Dominio.Recetas;

/// <summary>
/// Nombre de ingrediente, normalizado.
/// </summary>
/// <remarks>
/// De esta normalización depende que el catálogo no se duplique y, por tanto, que
/// la búsqueda por ingredientes de la feature 006 funcione: si "Tomate", "tomate"
/// y " tomate  frito " fueran filas distintas, preguntar por un ingrediente
/// devolvería resultados incompletos sin que nada avisara.
/// <para>
/// Se normaliza recortando los extremos, colapsando los espacios interiores y
/// pasando a minúsculas. No se quitan los acentos: en español distinguen palabras,
/// y "anís" no debería colapsar con "anis" por decisión nuestra.
/// </para>
/// </remarks>
public sealed partial class NombreDeIngrediente : IEquatable<NombreDeIngrediente>
{
    public const int LongitudMaxima = 80;

    private NombreDeIngrediente(string valor) => Valor = valor;

    public string Valor { get; }

    public static bool TryCrear(string? entrada, out NombreDeIngrediente nombre)
    {
        nombre = null!;

        if (string.IsNullOrWhiteSpace(entrada))
        {
            return false;
        }

        var normalizado = EspaciosSeguidos()
            .Replace(entrada.Trim(), " ")
            .ToLowerInvariant();

        if (normalizado.Length > LongitudMaxima)
        {
            return false;
        }

        nombre = new NombreDeIngrediente(normalizado);
        return true;
    }

    public static NombreDeIngrediente Crear(string entrada) =>
        TryCrear(entrada, out var nombre)
            ? nombre
            : throw new ArgumentException($"Nombre de ingrediente no válido: '{entrada}'.", nameof(entrada));

    public bool Equals(NombreDeIngrediente? otro) =>
        otro is not null && string.Equals(Valor, otro.Valor, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as NombreDeIngrediente);

    public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Valor;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex EspaciosSeguidos();
}
