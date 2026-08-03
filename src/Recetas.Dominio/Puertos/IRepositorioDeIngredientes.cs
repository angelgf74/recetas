using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeIngredientes
{
    /// <summary>
    /// Devuelve los ingredientes del catálogo que coincidan con esos nombres.
    /// </summary>
    Task<IReadOnlyCollection<Ingrediente>> BuscarPorNombresAsync(
        IReadOnlyCollection<NombreDeIngrediente> nombres,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Añade al catálogo los ingredientes que aún no existían.
    /// </summary>
    /// <remarks>
    /// La implementación debe tolerar que otra petición simultánea haya creado el
    /// mismo nombre entre la consulta y la inserción: el índice único de la base
    /// de datos es lo que garantiza que no haya duplicados, y el conflicto se
    /// resuelve releyendo en lugar de propagando el error.
    /// </remarks>
    Task<IReadOnlyCollection<Ingrediente>> AnadirNuevosAsync(
        IReadOnlyCollection<Ingrediente> ingredientes,
        CancellationToken cancelacion = default);
}
