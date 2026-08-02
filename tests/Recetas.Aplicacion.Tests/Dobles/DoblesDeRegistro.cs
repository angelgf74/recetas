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

    public Task<bool> ExisteConCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.Any(usuario => usuario.Correo.Equals(correo)));

    public Task AnadirAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }
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

    public Task GuardarCambiosAsync(CancellationToken cancelacion = default) => Task.CompletedTask;
}

/// <summary>Registra los envíos en lugar de hacerlos, para poder afirmar sobre ellos.</summary>
public sealed class EnviadorDeCorreoEspia : IEnviadorDeCorreo
{
    public List<(string Destinatario, string Enlace)> EnlacesDeAlta { get; } = [];

    public List<string> AvisosDeCuentaExistente { get; } = [];

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
