using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeRecetasEf(RecetasDbContext contexto) : IRepositorioDeRecetas
{
    public Task<Receta?> BuscarPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        contexto.Recetas
            // Carga explícita de las líneas y de su ingrediente: sin esto, pintar
            // la ficha dispararía una consulta por ingrediente.
            .Include(receta => receta.Ingredientes)
            .ThenInclude(linea => linea.Ingrediente)
            .FirstOrDefaultAsync(receta => receta.Id == id, cancelacion);

    public async Task<IReadOnlyCollection<Receta>> ListarPorAutorAsync(
        Guid autorId,
        CancellationToken cancelacion = default) =>
        // Sin Include: el listado no muestra ingredientes, y traerlos multiplicaría
        // las filas leídas sin que nadie las use.
        await contexto.Recetas
            .Where(receta => receta.AutorId == autorId)
            .OrderByDescending(receta => receta.FechaDeModificacion)
            .ToListAsync(cancelacion);

    public async Task AnadirAsync(Receta receta, CancellationToken cancelacion = default)
    {
        await contexto.Recetas.AddAsync(receta, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public async Task BorrarAsync(Receta receta, CancellationToken cancelacion = default)
    {
        // Las líneas caen por la cascada declarada en la configuración.
        contexto.Recetas.Remove(receta);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        contexto.SaveChangesAsync(cancelacion);
}
