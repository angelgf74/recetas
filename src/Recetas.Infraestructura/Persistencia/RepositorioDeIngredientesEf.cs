using Microsoft.EntityFrameworkCore;
using Npgsql;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeIngredientesEf(RecetasDbContext contexto) : IRepositorioDeIngredientes
{
    /// <summary>Código de PostgreSQL para violación de restricción única.</summary>
    private const string ViolacionDeUnicidad = "23505";

    public async Task<IReadOnlyCollection<Ingrediente>> BuscarPorNombresAsync(
        IReadOnlyCollection<NombreDeIngrediente> nombres,
        CancellationToken cancelacion = default)
    {
        if (nombres.Count == 0)
        {
            return [];
        }

        // Se compara sobre el objeto valor completo, NO sobre `Nombre.Valor`.
        //
        // Para EF la columna es el objeto valor: el conversor se aplica al
        // parámetro, así que la igualdad de objetos sí sabe traducirla. Acceder a
        // `.Valor` dentro de la consulta, en cambio, es una propiedad de C# que no
        // existe en SQL, y falla en tiempo de ejecución con
        // "The LINQ expression could not be translated" — no al compilar.
        var lista = nombres.ToList();

        return await contexto.Ingredientes
            .Where(ingrediente => lista.Contains(ingrediente.Nombre))
            .ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyCollection<Ingrediente>> AnadirNuevosAsync(
        IReadOnlyCollection<Ingrediente> ingredientes,
        CancellationToken cancelacion = default)
    {
        if (ingredientes.Count == 0)
        {
            return [];
        }

        await contexto.Ingredientes.AddRangeAsync(ingredientes, cancelacion);

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return ingredientes;
        }
        catch (DbUpdateException excepcion) when (EsConflictoDeUnicidad(excepcion))
        {
            // Otra petición simultánea creó alguno de estos ingredientes entre la
            // consulta y la inserción. El índice único hace su trabajo; aquí solo
            // hay que quedarse con las filas que ya existen.
            //
            // Se descartan las entidades que este contexto intentó insertar: si
            // siguieran rastreadas, el siguiente SaveChanges reintentaría el mismo
            // INSERT y volvería a fallar.
            foreach (var entrada in contexto.ChangeTracker.Entries<Ingrediente>().ToList())
            {
                if (entrada.State == EntityState.Added)
                {
                    entrada.State = EntityState.Detached;
                }
            }

            var nombres = ingredientes.Select(ingrediente => ingrediente.Nombre).ToList();

            return await BuscarPorNombresAsync(nombres, cancelacion);
        }
    }

    private static bool EsConflictoDeUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is PostgresException { SqlState: ViolacionDeUnicidad };
}
