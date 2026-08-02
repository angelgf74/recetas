using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Persistencia;

/// <summary>
/// Adaptador del puerto <see cref="IComprobadorDeAlmacen"/> contra PostgreSQL.
/// </summary>
public sealed class ComprobadorDeAlmacenEf(RecetasDbContext contexto) : IComprobadorDeAlmacen
{
    public async Task<bool> RespondeAsync(CancellationToken cancelacion = default)
    {
        try
        {
            return await contexto.Database.CanConnectAsync(cancelacion);
        }
        catch (Exception)
        {
            // Que la base de datos no responda es un estado esperado del sistema,
            // no un fallo del que haya que informar hacia arriba: es justo lo que
            // este puerto existe para detectar. El endpoint lo traducirá a 503.
            return false;
        }
    }
}
