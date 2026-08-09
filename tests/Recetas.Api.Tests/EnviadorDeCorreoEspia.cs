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
    private readonly ConcurrentQueue<(string Destinatario, string Enlace)> _enlacesDeContrasena = new();
    private readonly ConcurrentQueue<string> _avisos = new();

    public IReadOnlyCollection<(string Destinatario, string Enlace)> EnlacesDeAlta => _enlaces;

    public IReadOnlyCollection<(string Destinatario, string Enlace)> EnlacesDeContrasena => _enlacesDeContrasena;

    public IReadOnlyCollection<string> AvisosDeCuentaExistente => _avisos;

    private readonly ConcurrentQueue<(string Destinatario, AvisoDeDenuncia Aviso)> _denuncias = new();

    public IReadOnlyCollection<(string Destinatario, AvisoDeDenuncia Aviso)> AvisosDeDenuncia => _denuncias;

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

    public Task EnviarEnlaceDeContrasenaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        _enlacesDeContrasena.Enqueue((destinatario.Valor, enlace));
        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeDenunciaAsync(
        CorreoElectronico destinatario,
        AvisoDeDenuncia aviso,
        CancellationToken cancelacion = default)
    {
        _denuncias.Enqueue((destinatario.Valor, aviso));
        return Task.CompletedTask;
    }

    private readonly ConcurrentQueue<(string Destinatario, string Receta)> _retiradas = new();

    public IReadOnlyCollection<(string Destinatario, string Receta)> AvisosDeRetirada => _retiradas;

    public Task EnviarAvisoDeRetiradaAsync(
        CorreoElectronico destinatario,
        string nombreDeLaReceta,
        CancellationToken cancelacion = default)
    {
        _retiradas.Enqueue((destinatario.Valor, nombreDeLaReceta));
        return Task.CompletedTask;
    }

    private readonly ConcurrentQueue<string> _bajas = new();

    public IReadOnlyCollection<string> ConfirmacionesDeBaja => _bajas;

    public Task EnviarConfirmacionDeBajaAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        _bajas.Enqueue(destinatario.Valor);
        return Task.CompletedTask;
    }

    /// <summary>Extrae el token del último enlace de alta enviado a ese destinatario.</summary>
    public string TokenEnviadoA(string destinatario) =>
        TokenDe(_enlaces, destinatario);

    /// <summary>Extrae el token del último enlace de contraseña enviado a ese destinatario.</summary>
    public string TokenDeContrasenaEnviadoA(string destinatario) =>
        TokenDe(_enlacesDeContrasena, destinatario);

    private static string TokenDe(
        IEnumerable<(string Destinatario, string Enlace)> envios,
        string destinatario)
    {
        var enlace = envios.Last(envio => envio.Destinatario == destinatario).Enlace;
        var consulta = System.Web.HttpUtility.ParseQueryString(new Uri(enlace).Query);

        return consulta["token"]
            ?? throw new InvalidOperationException($"El enlace no lleva token: {enlace}");
    }
}
