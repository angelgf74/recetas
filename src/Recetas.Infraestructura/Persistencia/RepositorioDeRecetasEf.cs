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
            // Las fotos también: son pocas por receta y la ficha siempre las
            // necesita, igual que el borrado, que debe conocerlas para borrar sus
            // archivos del disco.
            .Include(receta => receta.Fotos)
            .FirstOrDefaultAsync(receta => receta.Id == id, cancelacion);

    public async Task<IReadOnlyCollection<Receta>> ListarPorAutorAsync(
        Guid autorId,
        CancellationToken cancelacion = default) =>
        // Sin los ingredientes: el listado no los muestra, y traerlos multiplicaría
        // las filas leídas sin que nadie las use. Las fotos sí, desde la 009: el
        // listado pinta la portada, y sin ellas cargadas saldría siempre vacía.
        await contexto.Recetas
            .Include(receta => receta.Fotos)
            .Where(receta => receta.AutorId == autorId)
            .OrderByDescending(receta => receta.FechaDeModificacion)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyCollection<Receta>> BuscarAsync(
        Guid usuarioId,
        CriteriosDeBusqueda criterios,
        int maximo,
        CancellationToken cancelacion = default)
    {
        // El filtro de visibilidad es LO PRIMERO y va en la consulta.
        //
        // Se podría cargar todo y descartar después en memoria, y funcionaría
        // igual mientras el código fuera correcto. Pero entonces existiría un
        // momento en el que las recetas privadas de otros están cargadas y solo
        // una condición las separa de la respuesta. Aquí no llegan a salir de la
        // base de datos.
        var consulta = contexto.Recetas
            .Where(receta => receta.AutorId == usuarioId || receta.Visibilidad == Visibilidad.Publica);

        if (criterios.Nombre is { Length: > 0 } nombre)
        {
            // Contra la columna normalizada, que ya está en minúsculas y sin
            // acentos, igual que el texto de la consulta.
            consulta = consulta.Where(receta => receta.NombreParaBusqueda.Contains(nombre));
        }

        if (criterios.TipoDePlato is { } tipo)
        {
            consulta = consulta.Where(receta => receta.TipoDePlato == tipo);
        }

        // Un Where por ingrediente, no un Contains sobre la lista: así se exige
        // que estén TODOS. Un único Any con una lista significaría "cualquiera de
        // ellos", que es lo contrario de lo que se busca al afinar.
        foreach (var ingrediente in criterios.Ingredientes)
        {
            var buscado = ingrediente;

            consulta = consulta.Where(receta => receta.Ingredientes
                .Any(linea => linea.Ingrediente!.NombreParaBusqueda.Contains(buscado)));
        }

        return await consulta
            // Igual que en el recetario: hacen falta para la portada del resultado.
            // Va al final para no estorbar a los filtros de arriba.
            .Include(receta => receta.Fotos)
            .OrderByDescending(receta => receta.FechaDeModificacion)
            // Uno más que el tope: si vuelve, es que hay recorte.
            .Take(maximo + 1)
            .ToListAsync(cancelacion);
    }

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
