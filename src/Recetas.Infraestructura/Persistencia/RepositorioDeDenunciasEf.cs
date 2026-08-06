using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeDenunciasEf(RecetasDbContext contexto) : IRepositorioDeDenuncias
{
    public Task<bool> YaDenuncioAsync(
        Guid denuncianteId,
        Guid recetaId,
        CancellationToken cancelacion = default) =>
        contexto.Denuncias.AnyAsync(
            denuncia => denuncia.DenuncianteId == denuncianteId && denuncia.RecetaId == recetaId,
            cancelacion);

    public async Task AnadirAsync(Denuncia denuncia, CancellationToken cancelacion = default) =>
        await contexto.Denuncias.AddAsync(denuncia, cancelacion);

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        contexto.SaveChangesAsync(cancelacion);
}
