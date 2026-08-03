using Recetas.Aplicacion.Registro;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Registro;

namespace Recetas.Aplicacion.Tests.Registro;

public class CompletarRegistroTests
{
    private const string ContrasenaValida = "una-contrasena-larga";

    private readonly RepositorioDeUsuariosEnMemoria _usuarios = new();
    private readonly RepositorioDeSolicitudesEnMemoria _solicitudes = new();
    private readonly GeneradorDeTokensPredecible _tokens = new();
    private readonly EnviadorDeCorreoEspia _correo = new();
    private readonly HasheadorFalso _hasheador = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    private CompletarRegistro CasoDeUso => new(_usuarios, _solicitudes, _tokens, _hasheador, _reloj);

    [Fact]
    public async Task TokenValido_CreaElUsuario()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");

        var resultado = await CasoDeUso.EjecutarAsync(token, ContrasenaValida);

        Assert.Equal(ResultadoDeCompletarRegistro.Completado, resultado);
        var usuario = Assert.Single(_usuarios.Todos);
        Assert.Equal("alguien@ejemplo.com", usuario.Correo.Valor);

        // La contraseña pasa por el hasheador, no se guarda tal cual. Que el
        // resultado sea criptográficamente sólido se comprueba donde toca,
        // en HasheadorPbkdf2Tests: aquí el hasheador es un doble.
        Assert.NotEqual(ContrasenaValida, usuario.HashDeContrasena);
        Assert.Equal(_hasheador.Hashear(CrearContrasena(ContrasenaValida)), usuario.HashDeContrasena);
    }

    [Fact]
    public async Task ElMismoToken_NoSirveDosVeces()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");
        var casoDeUso = CasoDeUso;

        await casoDeUso.EjecutarAsync(token, ContrasenaValida);
        var segundoIntento = await casoDeUso.EjecutarAsync(token, ContrasenaValida);

        Assert.Equal(ResultadoDeCompletarRegistro.EnlaceNoValido, segundoIntento);
        Assert.Single(_usuarios.Todos);
    }

    [Fact]
    public async Task TokenCaducado_NoCreaCuenta()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");

        _reloj.Avanzar(SolicitudDeRegistro.Vigencia + TimeSpan.FromMinutes(1));

        var resultado = await CasoDeUso.EjecutarAsync(token, ContrasenaValida);

        Assert.Equal(ResultadoDeCompletarRegistro.EnlaceNoValido, resultado);
        Assert.Empty(_usuarios.Todos);
    }

    [Fact]
    public async Task TokenInexistente_DevuelveElMismoErrorQueUnoCaducado()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");
        _reloj.Avanzar(SolicitudDeRegistro.Vigencia + TimeSpan.FromMinutes(1));

        var caducado = await CasoDeUso.EjecutarAsync(token, ContrasenaValida);
        var inventado = await CasoDeUso.EjecutarAsync("token-que-nunca-existio", ContrasenaValida);

        // Distinguirlos permitiría sondear qué tokens existieron alguna vez.
        Assert.Equal(caducado, inventado);
    }

    [Fact]
    public async Task ContrasenaDemasiadoCorta_NoCreaCuenta()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");

        var resultado = await CasoDeUso.EjecutarAsync(token, "corta");

        Assert.Equal(ResultadoDeCompletarRegistro.ContrasenaNoValida, resultado);
        Assert.Empty(_usuarios.Todos);
    }

    [Fact]
    public async Task ContrasenaInvalida_NoConsumeElToken()
    {
        var token = await PedirAltaDe("alguien@ejemplo.com");
        var casoDeUso = CasoDeUso;

        await casoDeUso.EjecutarAsync(token, "corta");
        var reintento = await casoDeUso.EjecutarAsync(token, ContrasenaValida);

        // Equivocarse al elegir contraseña no puede quemar el enlace: si lo hiciera,
        // un error de tecleo obligaría a pedir el alta otra vez.
        Assert.Equal(ResultadoDeCompletarRegistro.Completado, reintento);
    }

    // global:: es necesario: existe un espacio de nombres Recetas.Aplicacion.Recetas
    // que, desde aquí, tapa la raíz `Recetas` en la resolución de nombres.
    private static global::Recetas.Dominio.Usuarios.Contrasena CrearContrasena(string valor)
    {
        Assert.True(global::Recetas.Dominio.Usuarios.Contrasena.TryCrear(valor, out var contrasena));
        return contrasena;
    }

    private async Task<string> PedirAltaDe(string correo)
    {
        var solicitar = new SolicitarRegistro(_usuarios, _solicitudes, _tokens, _correo, _reloj);
        await solicitar.EjecutarAsync(correo, token => token);

        return _tokens.UltimoTokenGenerado;
    }
}
