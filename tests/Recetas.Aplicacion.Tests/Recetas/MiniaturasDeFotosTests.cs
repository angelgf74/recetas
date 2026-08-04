using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Recetas;

/// <summary>
/// Miniaturas: generación al subir, generación perezosa para lo ya subido,
/// visibilidad heredada de la receta y borrado en pareja con el original.
/// </summary>
public class MiniaturasDeFotosTests
{
    private const long DiezMegas = 10 * 1024 * 1024;

    private readonly RepositorioDeRecetasEnMemoria _recetas = new();
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly AlmacenDeFotosEnMemoria _almacen = new();
    private readonly EscaladorFalso _escalador = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bruno = Guid.NewGuid();

    private GestionDeFotos Fotos =>
        new(_recetas, _almacen, new LimpiadorQueNoTocaNada(), _escalador, _reloj);

    private GestionDeRecetas RecetasDe =>
        new(_recetas, new ResolverIngredientes(_ingredientes), _almacen, _reloj);

    // ------------------------------------------------------------ Al subir

    [Fact]
    public async Task Subir_GuardaTambienLaMiniatura()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        Assert.True(_almacen.Contiene(fotoId));
        Assert.True(_almacen.ContieneMiniatura(fotoId));
        Assert.Equal(IEscaladorDeImagenes.AnchoDeMiniatura, _escalador.UltimoAnchoPedido);
        Assert.NotEqual(Guid.Empty, recetaId);
    }

    [Fact]
    public async Task Subir_SiFallaElAlmacen_NoDejaLaFilaColgada()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        _almacen.FallarAlGuardar = true;

        await Assert.ThrowsAsync<IOException>(() =>
            Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas));

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.Empty(receta!.Fotos);
    }

    // -------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_DevuelveLaMiniatura_NoElOriginal()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        var (resultado, foto) = await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.NotNull(foto);
        Assert.Equal(EscaladorFalso.Marca, await LeerAsync(foto.Contenido));
    }

    /// <summary>
    /// El caso de las fotos anteriores a la feature 009: están en disco sin
    /// miniatura, y pedirla debe crearla en lugar de responder que no existe.
    /// </summary>
    [Fact]
    public async Task Obtener_SinMiniaturaEnDisco_LaGeneraYLaGuarda()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);
        _almacen.OlvidarMiniatura(fotoId);

        var (resultado, foto) = await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.NotNull(foto);
        Assert.True(_almacen.ContieneMiniatura(fotoId));
    }

    [Fact]
    public async Task Obtener_DosVeces_NoLaRegenera()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);
        _almacen.OlvidarMiniatura(fotoId);

        var llamadasTrasSubir = _escalador.Llamadas;

        await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);
        var llamadasTrasLaPrimera = _escalador.Llamadas;

        await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);

        Assert.Equal(llamadasTrasSubir + 1, llamadasTrasLaPrimera);
        Assert.Equal(llamadasTrasLaPrimera, _escalador.Llamadas);
    }

    [Fact]
    public async Task Obtener_SinOriginalEnDisco_NoEncontrada()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        // Se va el archivo entero: no hay nada de lo que escalar.
        await _almacen.BorrarAsync(fotoId, TipoDeImagen.Jpeg);

        var (resultado, foto) = await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
        Assert.Null(foto);
    }

    [Fact]
    public async Task Obtener_ConOriginalIlegible_NoEncontrada()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);
        _almacen.OlvidarMiniatura(fotoId);
        _escalador.Rechazar = true;

        var (resultado, _) = await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, fotoId);

        // Servir el archivo entero en su lugar sería justo lo que esta feature evita.
        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
    }

    [Fact]
    public async Task Obtener_DeUnaFotoQueNoExiste_NoEncontrada()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var (resultado, _) = await Fotos.ObtenerMiniaturaAsync(_ana, recetaId, Guid.NewGuid());

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
    }

    // ----------------------------------------------------------- Visibilidad

    [Fact]
    public async Task Obtener_DeRecetaPrivadaAjena_NoEncontrada()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        var (resultado, foto) = await Fotos.ObtenerMiniaturaAsync(_bruno, recetaId, fotoId);

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
        Assert.Null(foto);
    }

    [Fact]
    public async Task Obtener_DeRecetaPublicadaAjena_SeSirve()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        Assert.Equal(
            ResultadoDeReceta.Correcto,
            await RecetasDe.CambiarVisibilidadAsync(_ana, recetaId, publicar: true));

        var (resultado, foto) = await Fotos.ObtenerMiniaturaAsync(_bruno, recetaId, fotoId);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.NotNull(foto);
    }

    /// <summary>
    /// La generación perezosa no puede convertirse en una puerta trasera: si
    /// alguien pide la miniatura de una receta privada ajena, ni se sirve ni se
    /// genera.
    /// </summary>
    [Fact]
    public async Task Obtener_DeRecetaPrivadaAjena_NiSiquieraLaGenera()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);
        _almacen.OlvidarMiniatura(fotoId);

        var llamadasAntes = _escalador.Llamadas;

        await Fotos.ObtenerMiniaturaAsync(_bruno, recetaId, fotoId);

        Assert.Equal(llamadasAntes, _escalador.Llamadas);
        Assert.False(_almacen.ContieneMiniatura(fotoId));
    }

    // --------------------------------------------------------------- Borrar

    [Fact]
    public async Task Borrar_SeLlevaLosDosArchivos()
    {
        var (recetaId, fotoId) = await SubirFotoAsync(_ana);

        Assert.Equal(ResultadoDeFoto.Correcto, await Fotos.BorrarAsync(_ana, recetaId, fotoId));

        Assert.False(_almacen.Contiene(fotoId));
        Assert.False(_almacen.ContieneMiniatura(fotoId));
    }

    // ------------------------------------------------------------ Utilidades

    private static MemoryStream Imagen() => new(DetectorDeImagenTests.Jpeg());

    private static async Task<byte[]> LeerAsync(Stream contenido)
    {
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria);

        return memoria.ToArray();
    }

    private async Task<Guid> CrearRecetaAsync(Guid autor)
    {
        var (_, receta) = await RecetasDe.CrearAsync(autor, new DatosDeReceta(
            "Tortilla",
            TipoDePlato.PlatoPrincipal,
            "Pasos",
            [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]));

        return receta!.Id;
    }

    private async Task<(Guid RecetaId, Guid FotoId)> SubirFotoAsync(Guid autor)
    {
        var recetaId = await CrearRecetaAsync(autor);
        var (resultado, foto) = await Fotos.SubirAsync(autor, recetaId, Imagen(), DiezMegas);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);

        return (recetaId, foto!.Id);
    }
}
