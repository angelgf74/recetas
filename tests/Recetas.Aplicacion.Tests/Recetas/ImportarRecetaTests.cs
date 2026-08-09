using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;

namespace Recetas.Aplicacion.Tests.Recetas;

public class ImportarRecetaTests
{
    private const string PaginaConReceta =
        """
        <html><head>
        <script type="application/ld+json">
        { "@type": "Recipe", "name": "Fabada", "recipeYield": "4",
          "recipeIngredient": ["500 g de fabes", "2 chorizos"],
          "recipeInstructions": "Cocer todo a fuego lento." }
        </script>
        </head><body></body></html>
        """;

    private readonly DescargadorFalso _descargador = new();
    private readonly LimpiadorQueNoTocaNada _limpiador = new();

    private ImportarReceta CasoDeUso => new(_descargador, _limpiador);

    [Fact]
    public async Task ConUnaPaginaQuePublicaLaReceta_DevuelveElBorrador()
    {
        _descargador.Html = PaginaConReceta;

        var (resultado, receta, origen) = await CasoDeUso.EjecutarAsync("https://ejemplo.com/fabada");

        Assert.Equal(ResultadoDeImportacion.Correcto, resultado);
        Assert.Equal("Fabada", receta!.Nombre);
        Assert.Equal(4, receta.Raciones);
        Assert.Equal(2, receta.Ingredientes.Count);
        Assert.Equal("https://ejemplo.com/fabada", origen!.ToString());
    }

    [Fact]
    public async Task SinPagina_DiceQueNoSePudoLeer()
    {
        _descargador.Html = null;

        var (resultado, receta, _) = await CasoDeUso.EjecutarAsync("https://ejemplo.com/nada");

        Assert.Equal(ResultadoDeImportacion.NoSePudoLeer, resultado);
        Assert.Null(receta);
    }

    [Fact]
    public async Task ConUnaPaginaSinReceta_LoDiceAparte()
    {
        _descargador.Html = "<html><body>Un blog cualquiera</body></html>";

        var (resultado, _, _) = await CasoDeUso.EjecutarAsync("https://ejemplo.com/blog");

        // Distinto de NoSePudoLeer: aquí sí se abrió, y el usuario merece saber que
        // el problema es la página y no la dirección.
        Assert.Equal(ResultadoDeImportacion.SinReceta, resultado);
    }

    // ------------------------------------------------------- Esquemas y forma

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("esto no es una url")]
    [InlineData("/recetas/1")]
    public async Task UnaDireccionMalFormada_SeRechaza(string? direccion)
    {
        var (resultado, _, _) = await CasoDeUso.EjecutarAsync(direccion);

        Assert.Equal(ResultadoDeImportacion.DireccionNoValida, resultado);
        Assert.Equal(0, _descargador.Llamadas);
    }

    /// <summary>
    /// <c>file://</c> leería archivos del servidor; los demás esquemas se han usado
    /// para hablar con servicios internos. Se cortan antes de llegar a la red.
    /// </summary>
    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://ejemplo.com/receta")]
    [InlineData("gopher://ejemplo.com:70/1")]
    public async Task UnEsquemaQueNoEsHttp_SeRechazaSinDescargar(string direccion)
    {
        var (resultado, _, _) = await CasoDeUso.EjecutarAsync(direccion);

        Assert.Equal(ResultadoDeImportacion.DireccionNoValida, resultado);
        Assert.Equal(0, _descargador.Llamadas);
    }

    [Theory]
    [InlineData("http://ejemplo.com/receta")]
    [InlineData("https://ejemplo.com/receta")]
    public async Task HttpYHttps_SeAdmiten(string direccion)
    {
        _descargador.Html = PaginaConReceta;

        var (resultado, _, _) = await CasoDeUso.EjecutarAsync(direccion);

        Assert.Equal(ResultadoDeImportacion.Correcto, resultado);
    }

    // ------------------------------------------------------------- Imagen (027)

    private const string PaginaConRecetaYFoto =
        """
        <html><head>
        <script type="application/ld+json">
        { "@type": "Recipe", "name": "Fabada", "recipeIngredient": ["500 g de fabes"],
          "recipeInstructions": "Cocer.", "image": "https://ejemplo.com/fabada.jpg" }
        </script>
        </head><body></body></html>
        """;

    /// <summary>Cabecera JPEG real, suficiente para que DetectorDeImagen la reconozca.</summary>
    private static readonly byte[] BytesDeUnaImagenJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    [Fact]
    public async Task ConImagenValida_ElBorradorLaTraeLimpia()
    {
        _descargador.Html = PaginaConRecetaYFoto;
        _descargador.Imagen = BytesDeUnaImagenJpeg;

        var (_, receta, _) = await CasoDeUso.EjecutarAsync(
            "https://ejemplo.com/fabada", maximoDeBytesDeImagen: 1024);

        Assert.NotNull(receta!.ImagenLimpia);
        Assert.Equal("https://ejemplo.com/fabada.jpg", _descargador.UltimaImagenPedida!.ToString());
    }

    [Fact]
    public async Task SinPresupuestoParaImagen_NoIntentaDescargarla()
    {
        // maximoDeBytesDeImagen = 0, el valor por defecto: sin presupuesto no se
        // pide nada, en vez de asumir un límite que nadie ha decidido.
        _descargador.Html = PaginaConRecetaYFoto;
        _descargador.Imagen = BytesDeUnaImagenJpeg;

        var (_, receta, _) = await CasoDeUso.EjecutarAsync("https://ejemplo.com/fabada");

        Assert.Null(receta!.ImagenLimpia);
        Assert.Null(_descargador.UltimaImagenPedida);
    }

    [Fact]
    public async Task SiLaImagenNoSePuedeDescargar_LaImportacionSigueSiendoCorrecta()
    {
        _descargador.Html = PaginaConRecetaYFoto;
        _descargador.Imagen = null; // simula que la descarga falla

        var (resultado, receta, _) = await CasoDeUso.EjecutarAsync(
            "https://ejemplo.com/fabada", maximoDeBytesDeImagen: 1024);

        Assert.Equal(ResultadoDeImportacion.Correcto, resultado);
        Assert.Equal("Fabada", receta!.Nombre);
        Assert.Null(receta.ImagenLimpia);
    }

    [Fact]
    public async Task SiLosBytesNoSonUnaImagenReconocida_SigueSiendoCorrectaYSinFoto()
    {
        // El servidor de origen puede decir lo que quiera en el Content-Type: lo
        // que decide es el contenido, y esto no son bytes de ninguna imagen.
        _descargador.Html = PaginaConRecetaYFoto;
        _descargador.Imagen = "<html>esto no es una imagen</html>"u8.ToArray();

        var (resultado, receta, _) = await CasoDeUso.EjecutarAsync(
            "https://ejemplo.com/fabada", maximoDeBytesDeImagen: 1024);

        Assert.Equal(ResultadoDeImportacion.Correcto, resultado);
        Assert.Null(receta!.ImagenLimpia);
    }

    [Fact]
    public async Task SiLaLimpiezaFalla_SigueSiendoCorrectaYSinFoto()
    {
        _descargador.Html = PaginaConRecetaYFoto;
        _descargador.Imagen = BytesDeUnaImagenJpeg;
        _limpiador.Rechazar = true;

        var (resultado, receta, _) = await CasoDeUso.EjecutarAsync(
            "https://ejemplo.com/fabada", maximoDeBytesDeImagen: 1024);

        Assert.Equal(ResultadoDeImportacion.Correcto, resultado);
        Assert.Null(receta!.ImagenLimpia);
    }

    [Fact]
    public async Task SinImagenEnLaPagina_NoIntentaDescargarNada()
    {
        _descargador.Html = PaginaConReceta; // sin "image"

        var (_, receta, _) = await CasoDeUso.EjecutarAsync(
            "https://ejemplo.com/fabada", maximoDeBytesDeImagen: 1024);

        Assert.Null(receta!.ImagenLimpia);
        Assert.Null(_descargador.UltimaImagenPedida);
    }

    /// <summary>Registra lo que le piden y devuelve lo que se le ponga.</summary>
    private sealed class DescargadorFalso : Dominio.Puertos.IDescargadorDePaginas
    {
        public string? Html { get; set; }

        /// <summary>Bytes que devuelve <see cref="DescargarImagenAsync"/>, o <c>null</c> para simular un fallo.</summary>
        public byte[]? Imagen { get; set; }

        public int Llamadas { get; private set; }

        public Uri? UltimaImagenPedida { get; private set; }

        public Task<string?> DescargarAsync(Uri direccion, CancellationToken cancelacion = default)
        {
            Llamadas++;
            return Task.FromResult(Html);
        }

        public Task<byte[]?> DescargarImagenAsync(
            Uri direccion, long maximoDeBytes, CancellationToken cancelacion = default)
        {
            UltimaImagenPedida = direccion;
            return Task.FromResult(Imagen);
        }
    }
}
