using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeSolicitudesDeRegistro
{
    Task<SolicitudDeRegistro?> BuscarPorHashDelTokenAsync(
        string hashDelToken,
        CancellationToken cancelacion = default);

    /// <summary>Solicitudes de ese correo que aún no se han consumido, para invalidarlas.</summary>
    Task<IReadOnlyCollection<SolicitudDeRegistro>> BuscarVivasPorCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancelacion = default);

    Task AnadirAsync(SolicitudDeRegistro solicitud, CancellationToken cancelacion = default);

    /// <summary>
    /// Persiste los cambios hechos sobre solicitudes ya existentes (consumirlas o invalidarlas).
    /// Explícito a propósito: si dependiera de que otro repositorio guarde por él,
    /// reordenar dos líneas del caso de uso dejaría un token consumido sin registrar,
    /// reutilizable para crear una segunda cuenta.
    /// </summary>
    Task GuardarCambiosAsync(CancellationToken cancelacion = default);
}
