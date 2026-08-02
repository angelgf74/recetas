using Recetas.Aplicacion.Registro;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Tests.Registro;

public class SolicitarRegistroTests
{
    private readonly RepositorioDeUsuariosEnMemoria _usuarios = new();
    private readonly RepositorioDeSolicitudesEnMemoria _solicitudes = new();
    private readonly GeneradorDeTokensPredecible _tokens = new();
    private readonly EnviadorDeCorreoEspia _correo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    private SolicitarRegistro CasoDeUso => new(_usuarios, _solicitudes, _tokens, _correo, _reloj);

    private static string Enlace(string token) => $"https://recetas.example/registro/completar?token={token}";

    [Fact]
    public async Task CorreoNuevo_CreaSolicitudYEnviaEnlace()
    {
        var resultado = await CasoDeUso.EjecutarAsync("Nuevo@Ejemplo.COM", Enlace);

        Assert.Equal(ResultadoDeSolicitud.Aceptada, resultado);
        Assert.Single(_solicitudes.Todas);
        var envio = Assert.Single(_correo.EnlacesDeAlta);

        // El correo se normaliza a minúsculas: la unicidad no puede depender
        // de cómo lo escriba el usuario.
        Assert.Equal("nuevo@ejemplo.com", envio.Destinatario);
        Assert.Contains(_tokens.UltimoTokenGenerado, envio.Enlace);
    }

    [Fact]
    public async Task CorreoInvalido_NoCreaNadaNiEnvia()
    {
        var resultado = await CasoDeUso.EjecutarAsync("esto-no-es-un-correo", Enlace);

        Assert.Equal(ResultadoDeSolicitud.CorreoInvalido, resultado);
        Assert.Empty(_solicitudes.Todas);
        Assert.Empty(_correo.EnlacesDeAlta);
    }

    [Fact]
    public async Task CorreoYaRegistrado_AvisaAlDuenoYNoCreaSolicitud()
    {
        await DarDeAltaUsuario("existente@ejemplo.com");

        var resultado = await CasoDeUso.EjecutarAsync("existente@ejemplo.com", Enlace);

        Assert.Equal(ResultadoDeSolicitud.Aceptada, resultado);
        Assert.Empty(_solicitudes.Todas);
        Assert.Empty(_correo.EnlacesDeAlta);
        Assert.Single(_correo.AvisosDeCuentaExistente);
    }

    /// <summary>
    /// El criterio de aceptación central del paso 1: la respuesta no puede
    /// depender de si el correo ya tenía cuenta.
    /// </summary>
    [Fact]
    public async Task ElResultado_EsIdentico_ExistaONoLaCuenta()
    {
        await DarDeAltaUsuario("existente@ejemplo.com");

        var conCuenta = await CasoDeUso.EjecutarAsync("existente@ejemplo.com", Enlace);
        var sinCuenta = await CasoDeUso.EjecutarAsync("nuevo@ejemplo.com", Enlace);

        Assert.Equal(conCuenta, sinCuenta);
    }

    [Fact]
    public async Task SolicitarDeNuevo_InvalidaElEnlaceAnterior()
    {
        var casoDeUso = CasoDeUso;

        await casoDeUso.EjecutarAsync("alguien@ejemplo.com", Enlace);
        var primera = _solicitudes.Todas.Single();

        _reloj.Avanzar(TimeSpan.FromMinutes(5));
        await casoDeUso.EjecutarAsync("alguien@ejemplo.com", Enlace);

        Assert.Equal(2, _solicitudes.Todas.Count);
        Assert.False(primera.EstaVigente(_reloj.Ahora));
        Assert.True(_solicitudes.Todas[1].EstaVigente(_reloj.Ahora));
    }

    private async Task DarDeAltaUsuario(string correo)
    {
        var usuario = Usuario.Crear(CorreoElectronico.Crear(correo), "hash", _reloj.Ahora);
        await _usuarios.AnadirAsync(usuario);
    }
}
