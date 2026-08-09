using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Recetas;

public class GestionDeFotosTests
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
        new(_recetas, new ResolverIngredientes(_ingredientes), new ResolverEtiquetas(new RepositorioDeEtiquetasEnMemoria()), _almacen, _reloj);

    private async Task<Guid> CrearRecetaAsync(Guid autor)
    {
        var (_, receta) = await RecetasDe.CrearAsync(autor, new DatosDeReceta(
            "Tortilla",
            TipoDePlato.PlatoPrincipal,
            "Pasos",
            [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]));

        return receta!.Id;
    }

    private static MemoryStream Imagen() => new(DetectorDeImagenTests.Jpeg());

    // ----------------------------------------------------------------- Subir

    [Fact]
    public async Task Subir_GuardaFilaYArchivo()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var (resultado, foto) = await Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.NotNull(foto);
        Assert.Equal(TipoDeImagen.Jpeg, foto.Tipo);
        Assert.True(_almacen.Contiene(foto.Id));

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.Single(receta!.Fotos);
    }

    [Fact]
    public async Task Subir_ALaRecetaDeOtro_NoGuardaNada()
    {
        var deAna = await CrearRecetaAsync(_ana);

        var (resultado, foto) = await Fotos.SubirAsync(_bruno, deAna, Imagen(), DiezMegas);

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
        Assert.Null(foto);
        Assert.Equal(0, _almacen.Total);
    }

    [Fact]
    public async Task Subir_RechazaLoQueNoEsImagen()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        using var texto = new MemoryStream("<html>no soy una imagen</html>"u8.ToArray());

        var (resultado, _) = await Fotos.SubirAsync(_ana, recetaId, texto, DiezMegas);

        Assert.Equal(ResultadoDeFoto.NoEsUnaImagen, resultado);
        Assert.Equal(0, _almacen.Total);
    }

    [Fact]
    public async Task Subir_RechazaArchivoVacio()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var (resultado, _) = await Fotos.SubirAsync(_ana, recetaId, new MemoryStream(), DiezMegas);

        Assert.Equal(ResultadoDeFoto.NoEsUnaImagen, resultado);
    }

    [Fact]
    public async Task Subir_RechazaArchivoDemasiadoGrande()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var (resultado, _) = await Fotos.SubirAsync(_ana, recetaId, Imagen(), tamanoMaximoEnBytes: 4);

        Assert.Equal(ResultadoDeFoto.DemasiadoGrande, resultado);
        Assert.Equal(0, _almacen.Total);
    }

    [Fact]
    public async Task Subir_SiFallaElArchivo_NoDejaLaFilaColgada()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        _almacen.FallarAlGuardar = true;

        await Assert.ThrowsAsync<IOException>(() =>
            Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas));

        // Una fila sin archivo daría un 404 al descargarla: se deshace.
        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.Empty(receta!.Fotos);
    }

    // -------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_DevuelveElContenido()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        var (_, foto) = await Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas);

        var (resultado, descargada) = await Fotos.ObtenerAsync(_ana, recetaId, foto!.Id);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.Equal(TipoDeImagen.Jpeg, descargada!.Tipo);
        Assert.True(descargada.Contenido.Length > 0);
    }

    [Fact]
    public async Task Obtener_NoDevuelveLaFotoDeOtroUsuario()
    {
        var deAna = await CrearRecetaAsync(_ana);
        var (_, foto) = await Fotos.SubirAsync(_ana, deAna, Imagen(), DiezMegas);

        var (resultado, descargada) = await Fotos.ObtenerAsync(_bruno, deAna, foto!.Id);

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
        Assert.Null(descargada);
    }

    [Fact]
    public async Task Obtener_FotoInexistente_DevuelveNoEncontrada()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var (resultado, _) = await Fotos.ObtenerAsync(_ana, recetaId, Guid.NewGuid());

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
    }

    // ---------------------------------------------------------------- Borrar

    [Fact]
    public async Task Borrar_QuitaFilaYArchivo()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        var (_, foto) = await Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas);

        var resultado = await Fotos.BorrarAsync(_ana, recetaId, foto!.Id);

        Assert.Equal(ResultadoDeFoto.Correcto, resultado);
        Assert.False(_almacen.Contiene(foto.Id));
        Assert.Equal(0, _almacen.Total);
    }

    [Fact]
    public async Task Borrar_NoTocaLaFotoDeOtroUsuario()
    {
        var deAna = await CrearRecetaAsync(_ana);
        var (_, foto) = await Fotos.SubirAsync(_ana, deAna, Imagen(), DiezMegas);

        var resultado = await Fotos.BorrarAsync(_bruno, deAna, foto!.Id);

        Assert.Equal(ResultadoDeFoto.NoEncontrada, resultado);
        Assert.True(_almacen.Contiene(foto.Id));
    }

    [Fact]
    public async Task BorrarLaReceta_BorraTambienLosArchivos()
    {
        var recetaId = await CrearRecetaAsync(_ana);
        await Fotos.SubirAsync(_ana, recetaId, Imagen(), DiezMegas);
        await Fotos.SubirAsync(_ana, recetaId, new MemoryStream(DetectorDeImagenTests.Png()), DiezMegas);

        Assert.Equal(2, _almacen.Total);

        await RecetasDe.BorrarAsync(_ana, recetaId);

        // La cascada de la base de datos borra las filas, pero no toca el disco:
        // sin este paso quedarían archivos que ninguna fila menciona.
        Assert.Equal(0, _almacen.Total);
    }
}
