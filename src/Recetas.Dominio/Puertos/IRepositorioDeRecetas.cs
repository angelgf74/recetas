using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeRecetas
{
    /// <summary>
    /// Busca una receta por su identificador, con los ingredientes cargados.
    /// </summary>
    /// <remarks>
    /// Devuelve la receta sea de quien sea: comprobar la autoría es cosa del caso
    /// de uso, con <see cref="Receta.EsDe"/>. Filtrar aquí por autor escondería la
    /// regla en la capa equivocada y haría imposible que la 005 sirva las públicas
    /// de otros.
    /// </remarks>
    Task<Receta?> BuscarPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Recetas de un autor, sin las líneas de ingrediente.</summary>
    Task<IReadOnlyCollection<Receta>> ListarPorAutorAsync(Guid autorId, CancellationToken cancelacion = default);

    Task AnadirAsync(Receta receta, CancellationToken cancelacion = default);

    Task BorrarAsync(Receta receta, CancellationToken cancelacion = default);

    Task GuardarCambiosAsync(CancellationToken cancelacion = default);
}
