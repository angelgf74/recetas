using System.Collections.Concurrent;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Api.Tests;

/// <summary>
/// Enviador de pruebas: guarda los mensajes en vez de entregarlos.
/// Ningún test debe mandar correo real, y es de aquí de donde se saca
/// el token que viaja dentro del enlace.
/// </summary>
public sealed class EnviadorDeCorreoEspia : IEnviadorDeCorreo
{
    private readonly ConcurrentQueue<(string Destinatario, string Enlace)> _enlaces = new();
    private readonly ConcurrentQueue<string> _avisos = new();

    public IReadOnlyCollection<(string Destinatario, string Enlace)> EnlacesDeAlta => _enlaces;

    public IReadOnlyCollection<string> AvisosDeCuentaExistente => _avisos;

    public Task EnviarEnlaceDeAltaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        _enlaces.Enqueue((destinatario.Valor, enlace));
        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeCuentaExistenteAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        _avisos.Enqueue(destinatario.Valor);
        return Task.CompletedTask;
    }

    /// <summary>Extrae el token del último enlace enviado a ese destinatario.</summary>
    public string TokenEnviadoA(string destinatario)
    {
        var enlace = _enlaces.Last(envio => envio.Destinatario == destinatario).Enlace;
        var consulta = System.Web.HttpUtility.ParseQueryString(new Uri(enlace).Query);

        return consulta["token"]
            ?? throw new InvalidOperationException($"El enlace no lleva token: {enlace}");
    }
}
