using Microsoft.EntityFrameworkCore;
using Npgsql;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia;

/// <summary>Calco de <see cref="RepositorioDeIngredientesEf"/>.</summary>
public sealed class RepositorioDeEtiquetasEf(RecetasDbContext contexto) : IRepositorioDeEtiquetas
{
    private const string ViolacionDeUnicidad = "23505";

    public async Task<IReadOnlyCollection<Etiqueta>> BuscarPorNombresAsync(
        IReadOnlyCollection<NombreDeEtiqueta> nombres,
        CancellationToken cancelacion = default)
    {
        if (nombres.Count == 0)
        {
            return [];
        }

        // Comparación sobre el objeto valor completo, no sobre `.Valor`: mismo
        // motivo que en `RepositorioDeIngredientesEf`.
        var lista = nombres.ToList();

        return await contexto.Etiquetas
            .Where(etiqueta => lista.Contains(etiqueta.Nombre))
            .ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyCollection<Etiqueta>> AnadirNuevasAsync(
        IReadOnlyCollection<Etiqueta> etiquetas,
        CancellationToken cancelacion = default)
    {
        if (etiquetas.Count == 0)
        {
            return [];
        }

        await contexto.Etiquetas.AddRangeAsync(etiquetas, cancelacion);

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return etiquetas;
        }
        catch (DbUpdateException excepcion) when (EsConflictoDeUnicidad(excepcion))
        {
            // Otra petición simultánea creó alguna de estas etiquetas entre la
            // consulta y la inserción. Se descartan las entidades que este
            // contexto intentó insertar, para que el siguiente SaveChanges no
            // reintente el mismo INSERT.
            foreach (var entrada in contexto.ChangeTracker.Entries<Etiqueta>().ToList())
            {
                if (entrada.State == EntityState.Added)
                {
                    entrada.State = EntityState.Detached;
                }
            }

            var nombres = etiquetas.Select(etiqueta => etiqueta.Nombre).ToList();

            return await BuscarPorNombresAsync(nombres, cancelacion);
        }
    }

    private static bool EsConflictoDeUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is PostgresException { SqlState: ViolacionDeUnicidad };
}
