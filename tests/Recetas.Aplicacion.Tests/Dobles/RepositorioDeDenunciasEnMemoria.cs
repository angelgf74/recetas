using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Puertos;

namespace Recetas.Aplicacion.Tests.Dobles;

public sealed class RepositorioDeDenunciasEnMemoria : IRepositorioDeDenuncias
{
    private readonly List<Denuncia> _denuncias = [];

    public IReadOnlyList<Denuncia> Todas => _denuncias;

    public Task<bool> YaDenuncioAsync(
        Guid denuncianteId,
        Guid recetaId,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_denuncias.Any(denuncia =>
            denuncia.DenuncianteId == denuncianteId && denuncia.RecetaId == recetaId));

    public Task AnadirAsync(Denuncia denuncia, CancellationToken cancelacion = default)
    {
        _denuncias.Add(denuncia);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) => Task.CompletedTask;
}
