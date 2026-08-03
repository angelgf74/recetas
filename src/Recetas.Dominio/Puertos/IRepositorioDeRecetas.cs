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

    /// <summary>
    /// Busca entre lo que ese usuario puede ver: sus recetas —privadas o
    /// públicas— y las publicadas por otros.
    /// </summary>
    /// <remarks>
    /// El filtro de visibilidad es responsabilidad de la implementación y debe ir
    /// <b>dentro de la consulta</b>, no descartando después en memoria. Buscar es
    /// la primera operación que mira muchas recetas de golpe, y filtrar al final
    /// dejaría abierto que una refactorización devolviera la lista antes de
    /// aplicar la condición.
    /// </remarks>
    /// <param name="maximo">Tope de resultados. Se pide uno más para saber si hay recorte.</param>
    Task<IReadOnlyCollection<Receta>> BuscarAsync(
        Guid usuarioId,
        CriteriosDeBusqueda criterios,
        int maximo,
        CancellationToken cancelacion = default);

    Task AnadirAsync(Receta receta, CancellationToken cancelacion = default);

    Task BorrarAsync(Receta receta, CancellationToken cancelacion = default);

    Task GuardarCambiosAsync(CancellationToken cancelacion = default);
}
