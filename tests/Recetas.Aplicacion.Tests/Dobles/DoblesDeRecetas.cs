using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Dobles;

/// <param name="catalogo">
/// Catálogo para resolver los nombres de ingrediente al buscar. El dominio solo
/// guarda el identificador en la línea; en el repositorio real la relación la
/// resuelve EF, y aquí hay que dársela.
/// </param>
public sealed class RepositorioDeRecetasEnMemoria(RepositorioDeIngredientesEnMemoria? catalogo = null)
    : IRepositorioDeRecetas
{
    private readonly List<Receta> _recetas = [];

    public IReadOnlyList<Receta> Todas => _recetas;

    public Task<Receta?> BuscarPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_recetas.FirstOrDefault(receta => receta.Id == id));

    public Task<IReadOnlyCollection<Receta>> ListarPorAutorAsync(
        Guid autorId,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyCollection<Receta>>(
            _recetas.Where(receta => receta.AutorId == autorId).ToList());

    /// <summary>
    /// En memoria las entidades ya están completas, así que devuelve lo mismo.
    /// La diferencia entre los dos métodos existe en el repositorio real, donde
    /// uno omite los ingredientes para no leer filas de más.
    /// </summary>
    public Task<IReadOnlyCollection<Receta>> ListarCompletasPorAutorAsync(
        Guid autorId,
        CancellationToken cancelacion = default) =>
        ListarPorAutorAsync(autorId, cancelacion);

    /// <summary>
    /// Reproduce el filtro de visibilidad y los criterios del repositorio real.
    /// </summary>
    /// <remarks>
    /// Que sea un doble no lo exime de la regla que importa: no devolver recetas
    /// privadas ajenas. Si aquí se relajara, los tests de aplicación pasarían con
    /// una implementación real insegura.
    /// </remarks>
    public Task<IReadOnlyCollection<Receta>> BuscarAsync(
        Guid usuarioId,
        CriteriosDeBusqueda criterios,
        int maximo,
        CancellationToken cancelacion = default)
    {
        var visibles = _recetas.Where(receta => receta.PuedeVerla(usuarioId));

        if (criterios.Nombre is { Length: > 0 } nombre)
        {
            visibles = visibles.Where(receta => receta.NombreParaBusqueda.Contains(nombre));
        }

        if (criterios.TipoDePlato is { } tipo)
        {
            visibles = visibles.Where(receta => receta.TipoDePlato == tipo);
        }

        // Todos los ingredientes, no cualquiera de ellos.
        foreach (var ingrediente in criterios.Ingredientes)
        {
            var buscado = ingrediente;

            visibles = visibles.Where(receta => receta.Ingredientes
                .Any(linea => catalogo?.NombreDeBusquedaDe(linea.IngredienteId)?.Contains(buscado) == true));
        }

        return Task.FromResult<IReadOnlyCollection<Receta>>(
            visibles
                .OrderByDescending(receta => receta.FechaDeModificacion)
                .Take(maximo + 1)
                .ToList());
    }

    public Task AnadirAsync(Receta receta, CancellationToken cancelacion = default)
    {
        _recetas.Add(receta);
        return Task.CompletedTask;
    }

    public Task BorrarAsync(Receta receta, CancellationToken cancelacion = default)
    {
        _recetas.Remove(receta);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) => Task.CompletedTask;
}

/// <summary>
/// Catálogo en memoria que respeta la regla que importa: un nombre normalizado,
/// una sola entidad.
/// </summary>
public sealed class RepositorioDeIngredientesEnMemoria : IRepositorioDeIngredientes
{
    private readonly Dictionary<NombreDeIngrediente, Ingrediente> _catalogo = [];

    public int Total => _catalogo.Count;

    /// <summary>Nombre normalizado de un ingrediente por su identificador.</summary>
    public string? NombreDeBusquedaDe(Guid ingredienteId) =>
        _catalogo.Values.FirstOrDefault(ingrediente => ingrediente.Id == ingredienteId)?.NombreParaBusqueda;

    public Task<IReadOnlyCollection<Ingrediente>> BuscarPorNombresAsync(
        IReadOnlyCollection<NombreDeIngrediente> nombres,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyCollection<Ingrediente>>(
            nombres.Where(_catalogo.ContainsKey).Select(nombre => _catalogo[nombre]).ToList());

    public Task<IReadOnlyCollection<Ingrediente>> AnadirNuevosAsync(
        IReadOnlyCollection<Ingrediente> ingredientes,
        CancellationToken cancelacion = default)
    {
        foreach (var ingrediente in ingredientes)
        {
            _catalogo.TryAdd(ingrediente.Nombre, ingrediente);
        }

        return Task.FromResult<IReadOnlyCollection<Ingrediente>>(
            ingredientes.Select(ingrediente => _catalogo[ingrediente.Nombre]).ToList());
    }
}
