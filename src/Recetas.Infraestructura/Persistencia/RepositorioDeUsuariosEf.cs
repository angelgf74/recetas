using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia;

public sealed class RepositorioDeUsuariosEf(RecetasDbContext contexto) : IRepositorioDeUsuarios
{
    public Task<Usuario?> BuscarPorCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancelacion = default) =>
        contexto.Usuarios.FirstOrDefaultAsync(usuario => usuario.Correo == correo, cancelacion);

    public Task<bool> ExisteConCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancelacion = default) =>
        contexto.Usuarios.AnyAsync(usuario => usuario.Correo == correo, cancelacion);

    public async Task AnadirAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        await contexto.Usuarios.AddAsync(usuario, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
