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
    }

    /// <summary>Constructor para EF Core.</summary>
    private Ingrediente() => Nombre = null!;

    public Guid Id { get; private set; }

    public NombreDeIngrediente Nombre { get; private set; }

    public static Ingrediente Crear(NombreDeIngrediente nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return new Ingrediente(Guid.NewGuid(), nombre);
    }
}
