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

    /// <summary>
    /// Borra la cuenta.
    /// </summary>
    /// <remarks>
    /// <b>No arrastra sus recetas</b>: <c>recetas.autor_id</c> no tiene clave
    /// foránea a esta tabla, solo un índice, así que la base de datos no borra
    /// nada por su cuenta y las dejaría vivas y sin dueño. Quien llame aquí tiene
    /// que haberlas borrado antes, junto con los archivos de sus fotos.
    /// </remarks>
    Task BorrarAsync(Usuario usuario, CancellationToken cancelacion = default);
}
