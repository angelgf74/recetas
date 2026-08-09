using Recetas.Dominio.Puertos;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Tests.Dobles;

/// <summary>Reloj controlable: la caducidad es una regla de negocio y hay que poder viajar en el tiempo.</summary>
public sealed class RelojFalso(DateTimeOffset inicio) : IReloj
{
    public DateTimeOffset Ahora { get; private set; } = inicio;

    public void Avanzar(TimeSpan cuanto) => Ahora = Ahora.Add(cuanto);
}

public sealed class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly List<Usuario> _usuarios = [];

    public IReadOnlyList<Usuario> Todos => _usuarios;

    public Task<Usuario?> BuscarPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Correo.Equals(correo)));

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Id == id));

    public Task<bool> ExisteConCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.Any(usuario => usuario.Correo.Equals(correo)));

    public Task AnadirAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    /// <summary>Los objetos son los mismos que guarda la lista: mutarlos ya los "persiste".</summary>
    public Task ActualizarAsync(Usuario usuario, CancellationToken cancelacion = default) =>
        Task.CompletedTask;

    /// <summary>
    /// Borra solo al usuario. Igual que el repositorio real, <b>no arrastra sus
    /// recetas</b>: si este doble las borrara, un test pasaría con una
    /// implementación que deja recetas huérfanas.
    /// </summary>
    public Task BorrarAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        _usuarios.Remove(usuario);
        return Task.CompletedTask;
    }
}

public sealed class RepositorioDeSolicitudesDeContrasenaEnMemoria : IRepositorioDeSolicitudesDeContrasena
{
    private readonly List<SolicitudDeContrasena> _solicitudes = [];

    public IReadOnlyList<SolicitudDeContrasena> Todas => _solicitudes;

    public Task<SolicitudDeContrasena?> BuscarPorHashDelTokenAsync(
        string hashDelToken,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_solicitudes.FirstOrDefault(solicitud => solicitud.HashDelToken == hashDelToken));

    public Task<IReadOnlyCollection<SolicitudDeContrasena>> BuscarVivasPorUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyCollection<SolicitudDeContrasena>>(
            _solicitudes.Where(solicitud => solicitud.UsuarioId == usuarioId && !solicitud.EstaConsumida).ToList());

    public Task AnadirAsync(SolicitudDeContrasena solicitud, CancellationToken cancelacion = default)
    {
        _solicitudes.Add(solicitud);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) => Task.CompletedTask;
}

public sealed class RepositorioDeSolicitudesEnMemoria : IRepositorioDeSolicitudesDeRegistro
{
    private readonly List<SolicitudDeRegistro> _solicitudes = [];

    public IReadOnlyList<SolicitudDeRegistro> Todas => _solicitudes;

    public Task<SolicitudDeRegistro?> BuscarPorHashDelTokenAsync(
        string hashDelToken,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_solicitudes.FirstOrDefault(solicitud => solicitud.HashDelToken == hashDelToken));

    public Task<IReadOnlyCollection<SolicitudDeRegistro>> BuscarVivasPorCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyCollection<SolicitudDeRegistro>>(
            _solicitudes.Where(solicitud => solicitud.Correo.Equals(correo) && !solicitud.EstaConsumida).ToList());

    public Task AnadirAsync(SolicitudDeRegistro solicitud, CancellationToken cancelacion = default)
    {
        _solicitudes.Add(solicitud);
        return Task.CompletedTask;
    }

    public Task BorrarPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default)
    {
        _solicitudes.RemoveAll(solicitud => solicitud.Correo.Equals(correo));
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) => Task.CompletedTask;
}

/// <summary>Registra los envíos en lugar de hacerlos, para poder afirmar sobre ellos.</summary>
public sealed class EnviadorDeCorreoEspia : IEnviadorDeCorreo
{
    public List<(string Destinatario, string Enlace)> EnlacesDeAlta { get; } = [];

    public List<(string Destinatario, string Enlace)> EnlacesDeContrasena { get; } = [];

    public List<string> AvisosDeCuentaExistente { get; } = [];

    public List<(string Destinatario, AvisoDeDenuncia Aviso)> AvisosDeDenuncia { get; } = [];

    /// <summary>
    /// Si está a <c>true</c>, el envío revienta. Sirve para comprobar que una
    /// denuncia sobrevive a que el correo falle.
    /// </summary>
    public bool FallaAlEnviar { get; set; }

    public Task EnviarEnlaceDeAltaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        EnlacesDeAlta.Add((destinatario.Valor, enlace));
        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeCuentaExistenteAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        AvisosDeCuentaExistente.Add(destinatario.Valor);
        return Task.CompletedTask;
    }

    public Task EnviarEnlaceDeContrasenaAsync(
        CorreoElectronico destinatario,
        string enlace,
        CancellationToken cancelacion = default)
    {
        EnlacesDeContrasena.Add((destinatario.Valor, enlace));
        return Task.CompletedTask;
    }

    public List<string> ConfirmacionesDeBaja { get; } = [];

    public List<(string Destinatario, string Receta)> AvisosDeRetirada { get; } = [];

    public Task EnviarAvisoDeRetiradaAsync(
        CorreoElectronico destinatario,
        string nombreDeLaReceta,
        CancellationToken cancelacion = default)
    {
        if (FallaAlEnviar)
        {
            throw new InvalidOperationException("Fallo simulado del envío de correo.");
        }

        AvisosDeRetirada.Add((destinatario.Valor, nombreDeLaReceta));
        return Task.CompletedTask;
    }

    public Task EnviarConfirmacionDeBajaAsync(
        CorreoElectronico destinatario,
        CancellationToken cancelacion = default)
    {
        if (FallaAlEnviar)
        {
            throw new InvalidOperationException("Fallo simulado del envío de correo.");
        }

        ConfirmacionesDeBaja.Add(destinatario.Valor);
        return Task.CompletedTask;
    }

    public Task EnviarAvisoDeDenunciaAsync(
        CorreoElectronico destinatario,
        AvisoDeDenuncia aviso,
        CancellationToken cancelacion = default)
    {
        if (FallaAlEnviar)
        {
            throw new InvalidOperationException("Fallo simulado del envío de correo.");
        }

        AvisosDeDenuncia.Add((destinatario.Valor, aviso));
        return Task.CompletedTask;
    }
}

/// <summary>Tokens predecibles: los tests necesitan saber cuál se generó.</summary>
public sealed class GeneradorDeTokensPredecible : IGeneradorDeTokens
{
    private int _siguiente;

    public string UltimoTokenGenerado { get; private set; } = string.Empty;

    public string GenerarToken()
    {
        UltimoTokenGenerado = $"token-{++_siguiente}";
        return UltimoTokenGenerado;
    }

    public string Hashear(string token) => $"hash({token})";
}

/// <summary>Hasheador trivial: los tests de aplicación no prueban criptografía.</summary>
public sealed class HasheadorFalso : IHasheadorDeContrasenas
{
    public string HashSenuelo => "hash(senuelo)";

    public string Hashear(Contrasena contrasena) => $"hash({contrasena.Valor})";

    public bool Verificar(string contrasenaEnClaro, string hashAlmacenado) =>
        hashAlmacenado == $"hash({contrasenaEnClaro})";
}

public sealed class EmisorDeAccesoFalso : IEmisorDeAcceso
{
    public AccesoEmitido Emitir(Usuario usuario) =>
        new($"acceso-de-{usuario.Correo.Valor}", DateTimeOffset.UtcNow.AddDays(1));
}
