using Microsoft.Extensions.Logging;
using Recetas.Dominio.Puertos;

namespace Recetas.Aplicacion.Cuentas;

public enum ResultadoDeBaja
{
    Correcto,

    /// <summary>La cuenta ya no existe. Un token válido de una cuenta borrada llega aquí.</summary>
    NoEncontrada,

    /// <summary>La contraseña no es la suya. No se ha borrado nada.</summary>
    CredencialesIncorrectas
}

/// <summary>
/// Borra la cuenta y todo lo que cuelga de ella.
/// </summary>
/// <remarks>
/// <b>El borrado lo orquesta este caso de uso, no la base de datos.</b> Dos
/// motivos, y cualquiera de los dos basta: <c>recetas.autor_id</c> no tiene clave
/// foránea a <c>usuarios</c> —solo un índice—, así que borrar la fila del usuario
/// dejaría sus recetas vivas y sin dueño; y aunque la tuviera, ninguna cascada
/// toca los archivos del disco.
/// <para>
/// El orden es el mismo que en el borrado de una receta y por lo mismo:
/// <b>archivos primero, filas después</b>. Al revés quedarían fotos que ninguna
/// fila menciona, invisibles y ocupando espacio para siempre. Así, un fallo a
/// medias se arregla repitiendo la operación.
/// </para>
/// </remarks>
public sealed class BorrarCuenta(
    IRepositorioDeUsuarios usuarios,
    IRepositorioDeRecetas recetas,
    IRepositorioDeSolicitudesDeRegistro solicitudes,
    IAlmacenDeFotos almacenDeFotos,
    IHasheadorDeContrasenas hasheador,
    IEnviadorDeCorreo enviadorDeCorreo,
    ILogger<BorrarCuenta> registro)
{
    public async Task<ResultadoDeBaja> EjecutarAsync(
        Guid usuarioId,
        string contrasena,
        CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, cancelacion);

        if (usuario is null)
        {
            return ResultadoDeBaja.NoEncontrada;
        }

        // Tener la sesión abierta no basta: esto no se puede deshacer, y un
        // teléfono desbloqueado un momento no debe bastar para vaciar una cuenta.
        if (!hasheador.Verificar(contrasena, usuario.HashDeContrasena))
        {
            return ResultadoDeBaja.CredencialesIncorrectas;
        }

        // Se guarda antes de borrar: después ya no habrá a quién avisar.
        var correo = usuario.Correo;

        var suyas = await recetas.ListarPorAutorAsync(usuarioId, cancelacion);

        foreach (var receta in suyas)
        {
            foreach (var foto in receta.Fotos)
            {
                // El almacén trata "ya no existe" como éxito: el objetivo es que
                // deje de estar, y que alguien se haya adelantado no es un error.
                await almacenDeFotos.BorrarAsync(foto.Id, foto.Tipo, cancelacion);
            }
        }

        foreach (var receta in suyas)
        {
            // Las denuncias sobre estas recetas sí se van en cascada: esa clave
            // foránea existe.
            await recetas.BorrarAsync(receta, cancelacion);
        }

        // Los ingredientes NO se tocan: son catálogo compartido, y borrar los que
        // usaba esta cuenta rompería las recetas de los demás.

        await solicitudes.BorrarPorCorreoAsync(correo, cancelacion);
        await usuarios.BorrarAsync(usuario, cancelacion);

        await AvisarAsync(correo, cancelacion);

        return ResultadoDeBaja.Correcto;
    }

    private async Task AvisarAsync(
        Dominio.Usuarios.CorreoElectronico correo,
        CancellationToken cancelacion)
    {
        try
        {
            await enviadorDeCorreo.EnviarConfirmacionDeBajaAsync(correo, cancelacion);
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            // La cuenta ya no existe. Devolver un error haría creer que no se
            // borró, y no hay nada que reintentar.
            registro.LogError(excepcion, "No se pudo confirmar por correo una baja ya efectuada.");
        }
    }
}
