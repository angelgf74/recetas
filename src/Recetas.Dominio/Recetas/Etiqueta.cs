namespace Recetas.Dominio.Recetas;

/// <summary>
/// Una etiqueta libre del catálogo compartido: "sin gluten", "rápido", "de la abuela".
/// </summary>
/// <remarks>
/// Calco de <see cref="Ingrediente"/>: entidad propia y no texto dentro de la
/// receta porque buscar por etiqueta lo exige, del mismo modo que buscar por
/// ingrediente. No pertenece a ningún usuario ni a ninguna receta.
/// </remarks>
public sealed class Etiqueta
{
    private Etiqueta(Guid id, NombreDeEtiqueta nombre)
    {
        Id = id;
        Nombre = nombre;
        NombreParaBusqueda = TextoParaBusqueda.Normalizar(nombre.Valor);
    }

    /// <summary>Constructor para EF Core.</summary>
    private Etiqueta() => Nombre = null!;

    public Guid Id { get; private set; }

    public NombreDeEtiqueta Nombre { get; private set; }

    /// <summary>Sin acentos, para que buscar "rapido" encuentre "rápido".</summary>
    public string NombreParaBusqueda { get; private set; } = string.Empty;

    public static Etiqueta Crear(NombreDeEtiqueta nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return new Etiqueta(Guid.NewGuid(), nombre);
    }
}
