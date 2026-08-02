using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Registro;

/// <summary>
/// Paso 2 del alta: el usuario abre el enlace y elige contraseña.
/// Es el único camino por el que nace un <see cref="Usuario"/>.
/// </summary>
public sealed class CompletarRegistro(
    IRepositorioDeUsuarios usuarios,
    IRepositorioDeSolicitudesDeRegistro solicitudes,
    IGeneradorDeTokens generadorDeTokens,
    IHasheadorDeContrasenas hasheador,
    IReloj reloj)
{
    public async Task<ResultadoDeCompletarRegistro> EjecutarAsync(
        string token,
        string contrasenaIndicada,
        CancellationToken cancelacion = default)
    {
        if (!Contrasena.TryCrear(contrasenaIndicada, out var contrasena))
        {
            return ResultadoDeCompletarRegistro.ContrasenaNoValida;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return ResultadoDeCompletarRegistro.EnlaceNoValido;
        }

        var solicitud = await solicitudes.BuscarPorHashDelTokenAsync(
            generadorDeTokens.Hashear(token), cancelacion);

        // Inexistente, manipulado, caducado o ya usado comparten respuesta: un mensaje
        // distinto para cada caso permitiría sondear qué tokens existieron.
        if (solicitud is null || !solicitud.Consumir(reloj.Ahora))
        {
            return ResultadoDeCompletarRegistro.EnlaceNoValido;
        }

        // Carrera posible: dos solicitudes vivas para el mismo correo completadas a la vez.
        // La comprobación cubre el caso normal; el índice único de la base de datos
        // es la red de seguridad ante la simultaneidad real.
        if (await usuarios.ExisteConCorreoAsync(solicitud.Correo, cancelacion))
        {
            // Se persiste el consumo igualmente: el token ya se ha gastado y no
            // debe poder reintentarse.
            await solicitudes.GuardarCambiosAsync(cancelacion);
            return ResultadoDeCompletarRegistro.EnlaceNoValido;
        }

        var usuario = Usuario.Crear(solicitud.Correo, hasheador.Hashear(contrasena), reloj.Ahora);

        await usuarios.AnadirAsync(usuario, cancelacion);
        await solicitudes.GuardarCambiosAsync(cancelacion);

        return ResultadoDeCompletarRegistro.Completado;
    }
}

public enum ResultadoDeCompletarRegistro
{
    Completado,

    /// <summary>Token inexistente, manipulado, caducado o ya consumido.</summary>
    EnlaceNoValido,

    ContrasenaNoValida
}
