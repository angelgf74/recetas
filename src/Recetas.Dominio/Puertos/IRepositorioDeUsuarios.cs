using Recetas.Dominio.Usuarios;

namespace Recetas.Dominio.Puertos;

public interface IRepositorioDeUsuarios
{
    Task<Usuario?> BuscarPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default);

    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<bool> ExisteConCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default);

    Task AnadirAsync(Usuario usuario, CancellationToken cancelacion = default);

    /// <summary>Persiste los cambios sobre un usuario ya existente.</summary>
    Task ActualizarAsync(Usuario usuario, CancellationToken cancelacion = default);
}
