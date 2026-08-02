using Recetas.Dominio.Puertos;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Registro;

/// <summary>
/// Paso 1 del alta: alguien pide cuenta para un correo.
/// </summary>
/// <remarks>
/// Solo distingue entre "correo con formato inválido" y "solicitud aceptada".
/// Que el correo ya tenga cuenta <b>no</b> cambia la respuesta: si lo hiciera,
/// este endpoint sería un comprobador de qué direcciones están registradas.
/// </remarks>
public sealed class SolicitarRegistro(
    IRepositorioDeUsuarios usuarios,
    IRepositorioDeSolicitudesDeRegistro solicitudes,
    IGeneradorDeTokens generadorDeTokens,
    IEnviadorDeCorreo enviadorDeCorreo,
    IReloj reloj)
{
    public async Task<ResultadoDeSolicitud> EjecutarAsync(
        string correoIndicado,
        Func<string, string> construirEnlace,
        CancellationToken cancelacion = default)
    {
        if (!CorreoElectronico.TryCrear(correoIndicado, out var correo))
        {
            return ResultadoDeSolicitud.CorreoInvalido;
        }

        if (await usuarios.ExisteConCorreoAsync(correo, cancelacion))
        {
            // El dueño del buzón se entera del intento; quien lo hizo, no.
            await enviadorDeCorreo.EnviarAvisoDeCuentaExistenteAsync(correo, cancelacion);
            return ResultadoDeSolicitud.Aceptada;
        }

        var ahora = reloj.Ahora;

        // Pedir el alta otra vez invalida los enlaces anteriores: solo el último sirve.
        foreach (var anterior in await solicitudes.BuscarVivasPorCorreoAsync(correo, cancelacion))
        {
            anterior.Invalidar(ahora);
        }

        var token = generadorDeTokens.GenerarToken();
        var solicitud = SolicitudDeRegistro.Crear(correo, generadorDeTokens.Hashear(token), ahora);

        await solicitudes.AnadirAsync(solicitud, cancelacion);

        // El envío va dentro del caso de uso a propósito: si Brevo falla, el usuario
        // debe enterarse ahora, no quedarse esperando un correo que nunca llega.
        await enviadorDeCorreo.EnviarEnlaceDeAltaAsync(correo, construirEnlace(token), cancelacion);

        return ResultadoDeSolicitud.Aceptada;
    }
}

public enum ResultadoDeSolicitud
{
    /// <summary>Solicitud admitida. No dice si se creó una cuenta nueva o el correo ya existía.</summary>
    Aceptada,

    CorreoInvalido
}
