using Microsoft.Extensions.Logging.Abstractions;
using Recetas.Aplicacion.Moderacion;
using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Moderacion;

/// <summary>
/// Denunciar contenido: quién puede, qué se guarda y qué pasa cuando el correo
/// falla. Y la otra mitad de la feature, que es quién puede retirar lo denunciado.
/// </summary>
public class GestionDeDenunciasTests
{
    private const string CorreoDelResponsableDelServicio = "responsable@ejemplo.com";

    private readonly RepositorioDeRecetasEnMemoria _recetas = new();
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly RepositorioDeDenunciasEnMemoria _denuncias = new();
    private readonly AlmacenDeFotosEnMemoria _almacen = new();
    private readonly EnviadorDeCorreoEspia _correo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));

    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bruno = Guid.NewGuid();

    private GestionDeDenuncias Denuncias =>
        new(_recetas,
            _denuncias,
            _correo,
            new CorreoDelResponsable(CorreoDelResponsableDelServicio),
            _reloj,
            NullLogger<GestionDeDenuncias>.Instance);

    private GestionDeRecetas RecetasDe =>
        new(_recetas, new ResolverIngredientes(_ingredientes), _almacen, _reloj);

    // ------------------------------------------------------------- Denunciar

    [Fact]
    public async Task Denunciar_UnaPublicaAjena_LaGuardaYAvisa()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        var resultado = await Denuncias.DenunciarAsync(
            _bruno, recetaId, MotivoDeDenuncia.Spam, "Es publicidad encubierta.");

        Assert.Equal(ResultadoDeDenuncia.Correcto, resultado);

        var denuncia = Assert.Single(_denuncias.Todas);
        Assert.Equal(recetaId, denuncia.RecetaId);
        Assert.Equal(_bruno, denuncia.DenuncianteId);
        Assert.Equal(MotivoDeDenuncia.Spam, denuncia.Motivo);
        Assert.Equal("Es publicidad encubierta.", denuncia.Comentario);

        var (destinatario, aviso) = Assert.Single(_correo.AvisosDeDenuncia);
        Assert.Equal(CorreoDelResponsableDelServicio, destinatario);
        Assert.Equal(recetaId, aviso.RecetaId);
        Assert.Equal("Tortilla", aviso.NombreDeLaReceta);
    }

    [Fact]
    public async Task Denunciar_LaPropia_NoSeGuarda()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        var resultado = await Denuncias.DenunciarAsync(_ana, recetaId, MotivoDeDenuncia.Otro, null);

        Assert.Equal(ResultadoDeDenuncia.EsPropia, resultado);
        Assert.Empty(_denuncias.Todas);
        Assert.Empty(_correo.AvisosDeDenuncia);
    }

    [Fact]
    public async Task Denunciar_UnaPrivadaAjena_RespondeNoEncontrada()
    {
        // Privada: Bruno no debería poder ni verla, así que tampoco denunciarla.
        // Responder otra cosa le confirmaría que existe.
        var recetaId = await CrearRecetaAsync(_ana);

        var resultado = await Denuncias.DenunciarAsync(_bruno, recetaId, MotivoDeDenuncia.Otro, null);

        Assert.Equal(ResultadoDeDenuncia.NoEncontrada, resultado);
        Assert.Empty(_denuncias.Todas);
    }

    [Fact]
    public async Task Denunciar_UnaQueNoExiste_RespondeNoEncontrada()
    {
        var resultado = await Denuncias.DenunciarAsync(
            _bruno, Guid.NewGuid(), MotivoDeDenuncia.Otro, null);

        Assert.Equal(ResultadoDeDenuncia.NoEncontrada, resultado);
    }

    [Fact]
    public async Task Denunciar_DosVeces_NoDuplicaNiVuelveAAvisar()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        await Denuncias.DenunciarAsync(_bruno, recetaId, MotivoDeDenuncia.Spam, null);
        var segunda = await Denuncias.DenunciarAsync(_bruno, recetaId, MotivoDeDenuncia.Ofensivo, "Otra vez");

        // Al usuario se le responde que sí: para él ya está denunciada.
        Assert.Equal(ResultadoDeDenuncia.Correcto, segunda);
        Assert.Single(_denuncias.Todas);
        Assert.Single(_correo.AvisosDeDenuncia);
    }

    [Fact]
    public async Task Denunciar_DosUsuariosDistintos_SonDosDenuncias()
    {
        var recetaId = await CrearPublicaAsync(_ana);
        var carla = Guid.NewGuid();

        await Denuncias.DenunciarAsync(_bruno, recetaId, MotivoDeDenuncia.Spam, null);
        await Denuncias.DenunciarAsync(carla, recetaId, MotivoDeDenuncia.Spam, null);

        Assert.Equal(2, _denuncias.Todas.Count);
        Assert.Equal(2, _correo.AvisosDeDenuncia.Count);
    }

    [Fact]
    public async Task Denunciar_SiElCorreoFalla_LaDenunciaSigueGuardada()
    {
        var recetaId = await CrearPublicaAsync(_ana);
        _correo.FallaAlEnviar = true;

        var resultado = await Denuncias.DenunciarAsync(_bruno, recetaId, MotivoDeDenuncia.Ofensivo, null);

        // El aviso es un extra. Si se pierde, el usuario no tiene por qué enterarse
        // ni volver a intentarlo: su denuncia está registrada.
        Assert.Equal(ResultadoDeDenuncia.Correcto, resultado);
        Assert.Single(_denuncias.Todas);
    }

    [Fact]
    public async Task Denunciar_SinResponsableConfigurado_LaGuardaIgual()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        var sinResponsable = new GestionDeDenuncias(
            _recetas,
            _denuncias,
            _correo,
            new CorreoDelResponsable(null),
            _reloj,
            NullLogger<GestionDeDenuncias>.Instance);

        var resultado = await sinResponsable.DenunciarAsync(
            _bruno, recetaId, MotivoDeDenuncia.Otro, null);

        Assert.Equal(ResultadoDeDenuncia.Correcto, resultado);
        Assert.Single(_denuncias.Todas);
        Assert.Empty(_correo.AvisosDeDenuncia);
    }

    // ------------------------------------------------------ Retirar contenido
    //
    // Retirar por moderación tiene su propio caso de uso desde la 020, y sus
    // tests viven en `RetirarPorModeracionTests`. Aquí queda solo lo que sigue
    // siendo de `GestionDeRecetas`: que ser responsable no abra nada más.

    [Fact]
    public async Task Retirar_ElResponsableNoPuedeEditarNiBorrarRecetasAjenas()
    {
        // El riesgo de esta feature es que "puede moderar" se convierta en "puede
        // todo". Ser responsable solo abre despublicar.
        var recetaId = await CrearPublicaAsync(_ana);

        var edicion = await RecetasDe.ActualizarAsync(_bruno, recetaId, new DatosDeReceta(
            "Secuestrada",
            TipoDePlato.Postre,
            "Otros pasos",
            [new LineaDeIngrediente("Azúcar", 100m, Unidad.Gramo)]));

        var borrado = await RecetasDe.BorrarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeReceta.NoEncontrada, edicion);
        Assert.Equal(ResultadoDeReceta.NoEncontrada, borrado);

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.Equal("Tortilla", receta!.Nombre);
    }

    // ------------------------------------------------------------- Auxiliares

    private async Task<Guid> CrearRecetaAsync(Guid autor)
    {
        var (_, receta) = await RecetasDe.CrearAsync(autor, new DatosDeReceta(
            "Tortilla",
            TipoDePlato.PlatoPrincipal,
            "Pasos",
            [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]));

        return receta!.Id;
    }

    private async Task<Guid> CrearPublicaAsync(Guid autor)
    {
        var recetaId = await CrearRecetaAsync(autor);
        await RecetasDe.CambiarVisibilidadAsync(autor, recetaId, publicar: true);

        return recetaId;
    }
}
