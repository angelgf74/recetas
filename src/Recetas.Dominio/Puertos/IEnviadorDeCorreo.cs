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
}
