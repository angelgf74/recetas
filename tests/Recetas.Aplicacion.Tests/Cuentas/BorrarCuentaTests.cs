using Microsoft.Extensions.Logging.Abstractions;
using Recetas.Aplicacion.Cuentas;
using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Registro;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Recetas;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Tests.Cuentas;

/// <summary>
/// Darse de baja: qué se lleva por delante y qué sobrevive.
/// </summary>
public class BorrarCuentaTests
{
    private const string Contrasena = "una-contrasena-larga";
    private const long DiezMegas = 10 * 1024 * 1024;

    private readonly RepositorioDeUsuariosEnMemoria _usuarios = new();
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly RepositorioDeRecetasEnMemoria _recetas;
    private readonly RepositorioDeSolicitudesEnMemoria _solicitudes = new();
    private readonly AlmacenDeFotosEnMemoria _almacen = new();
    private readonly EnviadorDeCorreoEspia _correo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero));

    // El doble distingue la contraseña buena de la mala, que es lo único que hace
    // falta aquí. La criptografía de verdad se prueba en Infraestructura, y estos
    // tests no pueden verla: Aplicación no depende de ella.
    private readonly HasheadorFalso _hasheador = new();

    public BorrarCuentaTests() => _recetas = new RepositorioDeRecetasEnMemoria(_ingredientes);

    private BorrarCuenta Baja =>
        new(_usuarios,
            _recetas,
            _solicitudes,
            _almacen,
            _hasheador,
            _correo,
            NullLogger<BorrarCuenta>.Instance);

    private GestionDeRecetas RecetasDe =>
        new(_recetas, new ResolverIngredientes(_ingredientes), new ResolverEtiquetas(new RepositorioDeEtiquetasEnMemoria()), _almacen, _reloj);

    private GestionDeFotos Fotos =>
        new(_recetas, _almacen, new LimpiadorQueNoTocaNada(), new EscaladorFalso(), _reloj);

    [Fact]
    public async Task Borrar_ConLaContrasenaCorrecta_SeLlevaCuentaRecetasYArchivos()
    {
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");
        var (recetaId, fotoId) = await CrearRecetaConFotoAsync(usuario.Id);

        var resultado = await Baja.EjecutarAsync(usuario.Id, Contrasena);

        Assert.Equal(ResultadoDeBaja.Correcto, resultado);
        Assert.Empty(_usuarios.Todos);
        Assert.Null(await _recetas.BuscarPorIdAsync(recetaId));

        // Lo que la cascada de la base de datos nunca habría tocado.
        Assert.False(_almacen.Contiene(fotoId));
        Assert.False(_almacen.ContieneMiniatura(fotoId));
    }

    [Fact]
    public async Task Borrar_ConLaContrasenaEquivocada_NoBorraNada()
    {
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");
        var (recetaId, fotoId) = await CrearRecetaConFotoAsync(usuario.Id);

        var resultado = await Baja.EjecutarAsync(usuario.Id, "no-es-esta-contrasena");

        Assert.Equal(ResultadoDeBaja.CredencialesIncorrectas, resultado);
        Assert.Single(_usuarios.Todos);
        Assert.NotNull(await _recetas.BuscarPorIdAsync(recetaId));
        Assert.True(_almacen.Contiene(fotoId));
        Assert.Empty(_correo.ConfirmacionesDeBaja);
    }

    [Fact]
    public async Task Borrar_UnaCuentaQueYaNoEsta_RespondeNoEncontrada()
    {
        var resultado = await Baja.EjecutarAsync(Guid.NewGuid(), Contrasena);

        Assert.Equal(ResultadoDeBaja.NoEncontrada, resultado);
    }

    [Fact]
    public async Task Borrar_NoTocaLasRecetasDeOtros()
    {
        var ana = await CrearUsuarioAsync("ana@ejemplo.com");
        var bruno = await CrearUsuarioAsync("bruno@ejemplo.com");

        var deAna = await CrearRecetaConFotoAsync(ana.Id);
        var deBruno = await CrearRecetaConFotoAsync(bruno.Id);

        await Baja.EjecutarAsync(ana.Id, Contrasena);

        Assert.Null(await _recetas.BuscarPorIdAsync(deAna.RecetaId));
        Assert.NotNull(await _recetas.BuscarPorIdAsync(deBruno.RecetaId));
        Assert.True(_almacen.Contiene(deBruno.FotoId));
        Assert.Single(_usuarios.Todos);
    }

    [Fact]
    public async Task Borrar_DejaElCatalogoDeIngredientesIntacto()
    {
        // Son compartidos entre todos los usuarios: borrar los que usaba esta
        // cuenta rompería las recetas de los demás.
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");
        await CrearRecetaConFotoAsync(usuario.Id);

        var antes = _ingredientes.Total;
        Assert.True(antes > 0, "El test no prueba nada si el catálogo estaba vacío.");

        await Baja.EjecutarAsync(usuario.Id, Contrasena);

        Assert.Equal(antes, _ingredientes.Total);
    }

    [Fact]
    public async Task Borrar_SeLlevaLasSolicitudesDeAltaPendientes()
    {
        // Si sobrevivieran, un enlace de alta anterior a la baja podría volver a
        // crear la cuenta que su dueño acaba de borrar.
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");

        await _solicitudes.AnadirAsync(SolicitudDeRegistro.Crear(
            CorreoElectronico.Crear("ana@ejemplo.com"), "hash-del-token", _reloj.Ahora));

        Assert.Single(_solicitudes.Todas);

        await Baja.EjecutarAsync(usuario.Id, Contrasena);

        Assert.Empty(_solicitudes.Todas);
    }

    [Fact]
    public async Task Borrar_AvisaAlCorreoDeLaCuenta()
    {
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");

        await Baja.EjecutarAsync(usuario.Id, Contrasena);

        Assert.Equal("ana@ejemplo.com", Assert.Single(_correo.ConfirmacionesDeBaja));
    }

    [Fact]
    public async Task Borrar_SiElCorreoFalla_LaCuentaSeBorraIgual()
    {
        var usuario = await CrearUsuarioAsync("ana@ejemplo.com");
        _correo.FallaAlEnviar = true;

        var resultado = await Baja.EjecutarAsync(usuario.Id, Contrasena);

        // Devolver un error haría creer que la cuenta sigue ahí, y no hay nada
        // que reintentar: ya no existe.
        Assert.Equal(ResultadoDeBaja.Correcto, resultado);
        Assert.Empty(_usuarios.Todos);
    }

    // ------------------------------------------------------------- Auxiliares

    private async Task<Usuario> CrearUsuarioAsync(string correo)
    {
        var contrasena = Contrasena;
        Assert.True(Dominio.Usuarios.Contrasena.TryCrear(contrasena, out var valida));

        var usuario = Usuario.Crear(
            CorreoElectronico.Crear(correo),
            _hasheador.Hashear(valida),
            _reloj.Ahora);

        await _usuarios.AnadirAsync(usuario);

        return usuario;
    }

    private async Task<(Guid RecetaId, Guid FotoId)> CrearRecetaConFotoAsync(Guid autorId)
    {
        var (_, receta) = await RecetasDe.CrearAsync(autorId, new DatosDeReceta(
            "Tortilla",
            TipoDePlato.PlatoPrincipal,
            "Pasos",
            [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]));

        var (_, foto) = await Fotos.SubirAsync(
            autorId, receta!.Id, new MemoryStream(ImagenJpeg()), DiezMegas);

        return (receta.Id, foto!.Id);
    }

    /// <summary>Cabecera JPEG mínima: el detector solo mira los primeros bytes.</summary>
    private static byte[] ImagenJpeg() =>
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, .. new byte[64]];
}
