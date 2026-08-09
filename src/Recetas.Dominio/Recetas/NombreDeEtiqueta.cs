using System.Text.RegularExpressions;

namespace Recetas.Dominio.Recetas;

/// <summary>
/// Nombre de etiqueta, normalizado.
/// </summary>
/// <remarks>
/// Calco de <see cref="NombreDeIngrediente"/> y por el mismo motivo: sin esta
/// normalización, "Rápido", "rápido" y " rápido " serían tres filas distintas
/// del catálogo, y la búsqueda por etiqueta fallaría en silencio.
/// <para>
/// No se quitan los acentos aquí tampoco: eso vive en
/// <see cref="Etiqueta.NombreParaBusqueda"/>, igual que en <c>Ingrediente</c>.
/// </para>
/// </remarks>
public sealed partial class NombreDeEtiqueta : IEquatable<NombreDeEtiqueta>
{
    /// <summary>
    /// Más corto que el de un ingrediente: una etiqueta es una palabra o dos,
    /// no una descripción.
    /// </summary>
    public const int LongitudMaxima = 40;

    private NombreDeEtiqueta(string valor) => Valor = valor;

    public string Valor { get; }

    public static bool TryCrear(string? entrada, out NombreDeEtiqueta nombre)
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

        nombre = new NombreDeEtiqueta(normalizado);
        return true;
    }

    public static NombreDeEtiqueta Crear(string entrada) =>
        TryCrear(entrada, out var nombre)
            ? nombre
            : throw new ArgumentException($"Nombre de etiqueta no válido: '{entrada}'.", nameof(entrada));

    public bool Equals(NombreDeEtiqueta? otro) =>
        otro is not null && string.Equals(Valor, otro.Valor, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as NombreDeEtiqueta);

    public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Valor;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex EspaciosSeguidos();
}
