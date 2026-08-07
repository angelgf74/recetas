using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeSolicitudesDeRegistroEf(RecetasDbContext contexto)
    : IRepositorioDeSolicitudesDeRegistro
{
    public Task<SolicitudDeRegistro?> BuscarPorHashDelTokenAsync(
        string hashDelToken,
        CancellationToken cancelacion = default) =>
        contexto.SolicitudesDeRegistro
            .FirstOrDefaultAsync(solicitud => solicitud.HashDelToken == hashDelToken, cancelacion);

    public async Task<IReadOnlyCollection<SolicitudDeRegistro>> BuscarVivasPorCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancelacion = default) =>
        await contexto.SolicitudesDeRegistro
            .Where(solicitud => solicitud.Correo == correo && solicitud.FechaDeConsumo == null)
            .ToListAsync(cancelacion);

    public async Task AnadirAsync(SolicitudDeRegistro solicitud, CancellationToken cancelacion = default)
    {
        await contexto.SolicitudesDeRegistro.AddAsync(solicitud, cancelacion);

        // Guarda también las invalidaciones de solicitudes anteriores que el caso
        // de uso haya hecho sobre entidades ya rastreadas por el contexto.
        await contexto.SaveChangesAsync(cancelacion);
    }

    public Task BorrarPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default) =>
        // Borrado en el servidor: no hace falta traerlas para tirarlas.
        contexto.SolicitudesDeRegistro
            .Where(solicitud => solicitud.Correo == correo)
            .ExecuteDeleteAsync(cancelacion);

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        contexto.SaveChangesAsync(cancelacion);
}
