using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Favoritos;
using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeFavoritosEf(RecetasDbContext contexto) : IRepositorioDeFavoritos
{
    public Task<bool> EstaMarcadaAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default) =>
        contexto.Favoritos.AnyAsync(
            favorito => favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId,
            cancelacion);

    public async Task AnadirAsync(Favorito favorito, CancellationToken cancelacion = default) =>
        await contexto.Favoritos.AddAsync(favorito, cancelacion);

    public async Task QuitarAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        // Sin traer la fila antes: no hay nada que leer de ella y quitar lo que no
        // está tiene que ser válido. `ExecuteDelete` no toca el rastreador, así que
        // se ejecuta ya y no espera a `GuardarCambiosAsync`.
        await contexto.Favoritos
            .Where(favorito => favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId)
            .ExecuteDeleteAsync(cancelacion);
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        contexto.SaveChangesAsync(cancelacion);
}
