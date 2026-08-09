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

    /// <summary>
    /// Aviso al responsable del servicio de que alguien ha denunciado una receta
    /// pública.
    /// </summary>
    /// <remarks>
    /// No lleva quién denuncia. El responsable decide sobre el contenido, no sobre
    /// quién se quejó, y el dato está en la base si alguna vez hace falta
    /// distinguir un aviso suelto de una campaña.
    /// </remarks>
    Task EnviarAvisoDeDenunciaAsync(
        CorreoElectronico destinatario,
        AvisoDeDenuncia aviso,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Confirmación de que la cuenta se ha borrado.
    /// </summary>
    /// <remarks>
    /// Es el último mensaje que recibe esa dirección. Va después del borrado, así
    /// que cuando llega ya no queda nada suyo: si alguien lo recibe sin haberlo
    /// pedido, el aviso llega tarde para impedirlo pero a tiempo para que sepa que
    /// su contraseña estaba comprometida.
    /// </remarks>
    Task EnviarConfirmacionDeBajaAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Aviso al autor de que su receta ha dejado de estar publicada por decisión
    /// del responsable del servicio.
    /// </summary>
    /// <remarks>
    /// Lleva el nombre de la receta y nada más. <b>Ni quién denunció, ni cuántos,
    /// ni el motivo:</b> la retirada la decide el responsable —puede retirar sin
    /// que nadie haya denunciado— y en una comunidad pequeña decir cuántas
    /// denuncias hubo es casi decir quiénes.
    /// </remarks>
    Task EnviarAvisoDeRetiradaAsync(
        CorreoElectronico destinatario,
        string nombreDeLaReceta,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Confirmación de que la contraseña de la cuenta ha cambiado.
    /// </summary>
    /// <remarks>
    /// Quien lo pide ya tiene la sesión abierta, así que el correo no es la
    /// notificación que le informa: es la que avisa a quien <b>no</b> lo pidió de
    /// que alguien más conoce su contraseña actual y acaba de cambiarla.
    /// </remarks>
    Task EnviarConfirmacionDeCambioDeContrasenaAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default);
}

/// <param name="RecetaId">Identificador de la receta, para poder localizarla.</param>
/// <param name="NombreDeLaReceta">Su nombre, para reconocerla sin abrir nada.</param>
/// <param name="Motivo">Motivo elegido, ya en texto legible.</param>
/// <param name="Comentario">Explicación del denunciante, si la escribió.</param>
public sealed record AvisoDeDenuncia(
    Guid RecetaId,
    string NombreDeLaReceta,
    string Motivo,
    string? Comentario);
