using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

/// <summary>Calco de <see cref="IRepositorioDeIngredientes"/>, para el catálogo de etiquetas.</summary>
public interface IRepositorioDeEtiquetas
{
    Task<IReadOnlyCollection<Etiqueta>> BuscarPorNombresAsync(
        IReadOnlyCollection<NombreDeEtiqueta> nombres,
        CancellationToken cancelacion = default);

    Task<IReadOnlyCollection<Etiqueta>> AnadirNuevasAsync(
        IReadOnlyCollection<Etiqueta> etiquetas,
        CancellationToken cancelacion = default);
}
