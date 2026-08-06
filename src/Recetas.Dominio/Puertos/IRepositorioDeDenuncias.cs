using Recetas.Dominio.Moderacion;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeDenuncias
{
    /// <summary>
    /// Si ese usuario ya denunció esa receta. Evita repetir el aviso al responsable
    /// por algo que ya tiene encima de la mesa.
    /// </summary>
    Task<bool> YaDenuncioAsync(Guid denuncianteId, Guid recetaId, CancellationToken cancelacion = default);

    Task AnadirAsync(Denuncia denuncia, CancellationToken cancelacion = default);

    Task GuardarCambiosAsync(CancellationToken cancelacion = default);
}
