using Recetas.Dominio.Favoritos;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeFavoritos
{
    /// <summary>Si ese usuario tiene marcada esa receta.</summary>
    Task<bool> EstaMarcadaAsync(Guid usuarioId, Guid recetaId, CancellationToken cancelacion = default);

    Task AnadirAsync(Favorito favorito, CancellationToken cancelacion = default);

    /// <summary>
    /// Quita la marca. <b>Si no estaba, no pasa nada</b>: desmarcar dos veces es el
    /// mismo estado final, y fallar obligaría a la interfaz a conocer el estado
    /// antes de actuar.
    /// </summary>
    Task QuitarAsync(Guid usuarioId, Guid recetaId, CancellationToken cancelacion = default);

    Task GuardarCambiosAsync(CancellationToken cancelacion = default);
}
