using Microsoft.Extensions.Logging;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Correo;

/// <summary>
/// Enviador para desarrollo y pruebas: escribe el mensaje en el log en vez de enviarlo.
/// Es el registrado por defecto, para que ninguna máquina de desarrollo mande
/// correo real a direcciones ajenas por accidente.
/// </summary>
public sealed class EnviadorDeCorreoDeConsola(ILogger<EnviadorDeCorreoDeConsola> registro) : IEnviadorDeCorreo
{
    public Task EnviarEnlaceDeAltaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        // El enlace lleva el token dentro. Se escribe aquí a propósito: sin él no
        // habría forma de completar un alta en local. Por eso este enviador nunca
        // debe activarse en producción, donde ese log sería una fuga.
        registro.LogInformation(
            "[CORREO SIMULADO] Alta para {Destinatario}. Enlace: {Enlace}",
            destinatario.Valor,
            enlace);

        return Task.CompletedTask;
    }

    public Task EnviarEnlaceDeContrasenaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        registro.LogInformation(
            "[CORREO SIMULADO] Restablecer contraseña de {Destinatario}. Enlace: {Enlace}",
            destinatario.Valor,
            enlace);

        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeCuentaExistenteAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        registro.LogInformation(
            "[CORREO SIMULADO] Aviso de cuenta ya existente para {Destinatario}.",
            destinatario.Valor);

        return Task.CompletedTask;
    }

    public Task EnviarConfirmacionDeBajaAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        registro.LogInformation(
            "[CORREO SIMULADO] Cuenta borrada: {Destinatario}.",
            destinatario.Valor);

        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeDenunciaAsync(
        CorreoElectronico destinatario,
        AvisoDeDenuncia aviso,
        CancellationToken cancelacion = default)
    {
        registro.LogInformation(
            "[CORREO SIMULADO] Denuncia para {Destinatario}. Receta {Receta} ({Nombre}), motivo {Motivo}. {Comentario}",
            destinatario.Valor,
            aviso.RecetaId,
            aviso.NombreDeLaReceta,
            aviso.Motivo,
            aviso.Comentario ?? "(sin comentario)");

        return Task.CompletedTask;
    }
}
