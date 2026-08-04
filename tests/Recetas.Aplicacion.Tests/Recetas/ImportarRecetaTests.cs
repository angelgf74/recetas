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

    private ImportarReceta CasoDeUso => new(_descargador);

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

    /// <summary>Registra lo que le piden y devuelve lo que se le ponga.</summary>
    private sealed class DescargadorFalso : Dominio.Puertos.IDescargadorDePaginas
    {
        public string? Html { get; set; }

        public int Llamadas { get; private set; }

        public Task<string?> DescargarAsync(Uri direccion, CancellationToken cancelacion = default)
        {
            Llamadas++;
            return Task.FromResult(Html);
        }
    }
}
