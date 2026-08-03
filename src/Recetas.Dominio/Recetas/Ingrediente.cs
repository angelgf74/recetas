namespace Recetas.Dominio.Recetas;

/// <summary>
/// Un ingrediente del catálogo compartido: "tomate", "harina", "aceite de oliva".
/// </summary>
/// <remarks>
/// Es una entidad propia y no texto dentro de la receta porque la búsqueda por
/// ingredientes lo exige: solo compartiendo la misma fila se puede preguntar qué
/// recetas llevan tomate.
/// <para>
/// No pertenece a ningún usuario ni a ninguna receta. La cantidad y la unidad no
/// viven aquí sino en <see cref="IngredienteDeReceta"/>, que es el uso concreto.
/// </para>
/// </remarks>
public sealed class Ingrediente
{
    private Ingrediente(Guid id, NombreDeIngrediente nombre)
    {
        Id = id;
        Nombre = nombre;
        NombreParaBusqueda = TextoParaBusqueda.Normalizar(nombre.Valor);
    }

    /// <summary>Constructor para EF Core.</summary>
    private Ingrediente() => Nombre = null!;

    public Guid Id { get; private set; }

    public NombreDeIngrediente Nombre { get; private set; }

    /// <summary>
    /// El nombre sin acentos, para que buscar "pimenton" encuentre "pimentón".
    /// <see cref="Nombre"/> conserva los acentos porque es lo que se muestra.
    /// </summary>
    public string NombreParaBusqueda { get; private set; } = string.Empty;

    public static Ingrediente Crear(NombreDeIngrediente nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return new Ingrediente(Guid.NewGuid(), nombre);
    }
}
