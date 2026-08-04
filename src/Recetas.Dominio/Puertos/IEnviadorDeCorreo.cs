using Recetas.Dominio.Usuarios;

namespace Recetas.Dominio.Puertos;

/// <summary>
/// Entrega de correo. El dominio no sabe si detrás hay Brevo, SMTP o la consola.
/// </summary>
public interface IEnviadorDeCorreo
{
    /// <summary>Enlace para completar un alta nueva.</summary>
    Task EnviarEnlaceDeAltaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Aviso a un correo que ya tiene cuenta y sobre el que alguien ha pedido el alta.
    /// Es lo que permite responder igual en los dos casos sin dejar al dueño a oscuras.
    /// </summary>
    Task EnviarAvisoDeCuentaExistenteAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Enlace para elegir una contraseña nueva. Solo se envía a buzones que tienen
    /// cuenta: a los demás no se les manda nada, porque el mensaje no les serviría
    /// de nada y confirmaría a quien lo pidió que el intento llegó a alguna parte.
    /// </summary>
    Task EnviarEnlaceDeContrasenaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default);
}
