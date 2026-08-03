using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Dobles;

public sealed class RepositorioDeRecetasEnMemoria : IRepositorioDeRecetas
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
