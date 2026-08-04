using Recetas.Aplicacion.Recetas;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Recetas;

/// <summary>
/// Cada web escribe su JSON-LD a su manera, así que estos tests recorren las formas
/// reales que toma el mismo campo.
/// </summary>
public class LectorDeRecetaEnJsonLdTests
{
    private static string Pagina(string jsonLd) =>
        $$"""
          <!DOCTYPE html>
          <html><head><title>Da igual</title>
          <script type="application/ld+json">{{jsonLd}}</script>
          </head><body><p>Texto que no interesa</p></body></html>
          """;

    private const string RecetaCompleta =
        """
        {
          "@context": "https://schema.org",
          "@type": "Recipe",
          "name": "Tortilla de patatas",
          "recipeYield": "4 raciones",
          "recipeIngredient": ["6 huevos", "800 g de patatas", "sal al gusto"],
          "recipeInstructions": [
            { "@type": "HowToStep", "text": "Pelar y cortar las patatas." },
            { "@type": "HowToStep", "text": "Freírlas a fuego medio." },
            { "@type": "HowToStep", "text": "Batir los huevos y cuajar." }
          ]
        }
        """;

    [Fact]
    public void UnaRecetaCompleta_SeLeeEntera()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(RecetaCompleta));

        Assert.NotNull(receta);
        Assert.Equal("Tortilla de patatas", receta.Nombre);
        Assert.Equal(4, receta.Raciones);
        Assert.Equal(3, receta.Ingredientes.Count);
        Assert.Contains("Pelar y cortar las patatas.", receta.Elaboracion);
        Assert.Contains("Batir los huevos y cuajar.", receta.Elaboracion);
    }

    [Fact]
    public void LosIngredientes_LleganPartidos()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(RecetaCompleta));

        var patatas = receta!.Ingredientes.Single(linea => linea.Nombre == "patatas");

        Assert.Equal(800m, patatas.Cantidad);
        Assert.Equal(Unidad.Gramo, patatas.Unidad);
    }

    /// <summary>Sin esto, la receta importada saldría con ingredientes de menos.</summary>
    [Fact]
    public void UnIngredienteIlegible_NoSePierde()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(RecetaCompleta));

        Assert.Contains(receta!.Ingredientes, linea => linea.Nombre == "sal al gusto");
    }

    // ------------------------------------------------- Formas del mismo campo

    [Fact]
    public void ElTipoComoLista_SeReconoce()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            { "@type": ["Recipe", "NewsArticle"], "name": "Gazpacho",
              "recipeIngredient": ["1 kg de tomates"] }
            """));

        Assert.Equal("Gazpacho", receta!.Nombre);
    }

    [Fact]
    public void LaRecetaDentroDeUnGrafo_SeEncuentra()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            { "@context": "https://schema.org",
              "@graph": [
                { "@type": "WebSite", "name": "Un blog" },
                { "@type": "Recipe", "name": "Salmorejo", "recipeIngredient": ["500 g de tomate"] }
              ] }
            """));

        Assert.Equal("Salmorejo", receta!.Nombre);
    }

    [Fact]
    public void LaRecetaDentroDeUnaLista_SeEncuentra()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            [ { "@type": "Organization", "name": "Una empresa" },
              { "@type": "Recipe", "name": "Ajoblanco", "recipeIngredient": ["100 g de almendra"] } ]
            """));

        Assert.Equal("Ajoblanco", receta!.Nombre);
    }

    [Fact]
    public void LaElaboracionComoTextoSuelto_SeLee()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            { "@type": "Recipe", "name": "Pan",
              "recipeIngredient": ["500 g de harina"],
              "recipeInstructions": "Amasar y hornear." }
            """));

        Assert.Equal("Amasar y hornear.", receta!.Elaboracion);
    }

    [Fact]
    public void LaElaboracionDentroDeSecciones_SeLee()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            { "@type": "Recipe", "name": "Lasaña",
              "recipeIngredient": ["1 paquete de placas"],
              "recipeInstructions": [
                { "@type": "HowToSection", "itemListElement": [
                    { "@type": "HowToStep", "text": "Hacer la boloñesa." },
                    { "@type": "HowToStep", "text": "Montar las capas." } ] } ] }
            """));

        Assert.Contains("Hacer la boloñesa.", receta!.Elaboracion);
        Assert.Contains("Montar las capas.", receta.Elaboracion);
    }

    [Fact]
    public void LasEtiquetasHtmlDeLosPasos_SeQuitan()
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            """
            { "@type": "Recipe", "name": "Crema",
              "recipeIngredient": ["1 l de caldo"],
              "recipeInstructions": "<p>Triturar <strong>bien</strong>.</p>" }
            """));

        Assert.DoesNotContain("<", receta!.Elaboracion);
        Assert.Contains("Triturar", receta.Elaboracion);
    }

    [Theory]
    [InlineData("\"4\"", 4)]
    [InlineData("4", 4)]
    [InlineData("\"4 personas\"", 4)]
    [InlineData("[\"6\"]", 6)]
    public void LasRaciones_SeLeenEnSusVariasFormas(string valor, int esperadas)
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            $$"""
              { "@type": "Recipe", "name": "Algo", "recipeYield": {{valor}},
                "recipeIngredient": ["1 unidad de algo"] }
              """));

        Assert.Equal(esperadas, receta!.Raciones);
    }

    /// <summary>
    /// Un valor fuera del rango del dominio se descarta: colocarlo haría que el
    /// escalado de la feature 010 partiera de un número inventado.
    /// </summary>
    [Theory]
    [InlineData("\"0\"")]
    [InlineData("500")]
    [InlineData("\"varias\"")]
    public void UnasRacionesInservibles_SeDescartan(string valor)
    {
        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            $$"""
              { "@type": "Recipe", "name": "Algo", "recipeYield": {{valor}},
                "recipeIngredient": ["1 unidad de algo"] }
              """));

        Assert.Null(receta!.Raciones);
    }

    // -------------------------------------------------------- Sin receta

    [Fact]
    public void UnaPaginaSinJsonLd_NoDevuelveNada()
    {
        Assert.Null(LectorDeRecetaEnJsonLd.Leer("<html><body>Hola</body></html>"));
    }

    [Fact]
    public void UnaPaginaSinReceta_NoDevuelveNada()
    {
        Assert.Null(LectorDeRecetaEnJsonLd.Leer(Pagina(
            """{ "@type": "NewsArticle", "name": "Una noticia" }""")));
    }

    [Fact]
    public void UnaRecetaSinNombre_NoDevuelveNada()
    {
        // Sin nombre no hay receta: es el único campo imprescindible.
        Assert.Null(LectorDeRecetaEnJsonLd.Leer(Pagina(
            """{ "@type": "Recipe", "recipeIngredient": ["300 g de harina"] }""")));
    }

    /// <summary>
    /// Muchas webs llevan varios bloques y solo uno es la receta; que uno esté roto
    /// no puede tumbar la importación.
    /// </summary>
    [Fact]
    public void UnBloqueRoto_NoImpideLeerElSiguiente()
    {
        var html =
            """
            <html><head>
            <script type="application/ld+json">{ esto no es json </script>
            <script type="application/ld+json">
            { "@type": "Recipe", "name": "Migas", "recipeIngredient": ["500 g de pan"] }
            </script>
            </head><body></body></html>
            """;

        Assert.Equal("Migas", LectorDeRecetaEnJsonLd.Leer(html)!.Nombre);
    }

    [Fact]
    public void UnNombreLarguisimo_SeRecorta()
    {
        var largo = new string('a', 500);

        var receta = LectorDeRecetaEnJsonLd.Leer(Pagina(
            $$"""
              { "@type": "Recipe", "name": "{{largo}}", "recipeIngredient": ["1 unidad de algo"] }
              """));

        Assert.True(receta!.Nombre.Length <= Receta.LongitudMaximaDelNombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SinHtml_NoDevuelveNada(string html)
    {
        Assert.Null(LectorDeRecetaEnJsonLd.Leer(html));
    }
}
