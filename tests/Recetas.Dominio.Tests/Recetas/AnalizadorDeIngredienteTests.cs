using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class AnalizadorDeIngredienteTests
{
    [Theory]
    [InlineData("300 g de harina", 300, Unidad.Gramo, "harina")]
    [InlineData("300g de harina", 300, Unidad.Gramo, "harina")]
    [InlineData("2 huevos", 2, Unidad.Unidad, "huevos")]
    [InlineData("1 kg de patatas", 1, Unidad.Kilogramo, "patatas")]
    [InlineData("250 ml de leche", 250, Unidad.Mililitro, "leche")]
    [InlineData("2 cucharadas de aceite", 2, Unidad.Cucharada, "aceite")]
    [InlineData("3 dientes de ajo", 3, Unidad.Diente, "ajo")]
    [InlineData("1 hoja de laurel", 1, Unidad.Hoja, "laurel")]
    [InlineData("2 ramitas de perejil", 2, Unidad.Rama, "perejil")]
    [InlineData("1 taza de caldo", 1, Unidad.Taza, "caldo")]
    public void LasLineasHabituales_SeParten(
        string texto, decimal cantidad, Unidad unidad, string nombre)
    {
        var linea = AnalizadorDeIngrediente.Analizar(texto);

        Assert.NotNull(linea);
        Assert.Equal(cantidad, linea.Cantidad);
        Assert.Equal(unidad, linea.Unidad);
        Assert.Equal(nombre, linea.Nombre);
    }

    [Theory]
    [InlineData("1,5 kg de ternera", 1.5)]
    [InlineData("1.5 kg de ternera", 1.5)]
    [InlineData("1/2 kg de ternera", 0.5)]
    public void LosDecimalesYLasFracciones_SeEntienden(string texto, decimal esperada)
    {
        Assert.Equal(esperada, AnalizadorDeIngrediente.Analizar(texto)!.Cantidad);
    }

    /// <summary>
    /// Las webs escriben las fracciones con un solo carácter. Sin traducirlas, la
    /// línea entera se daría por ilegible y perdería la cantidad.
    /// </summary>
    [Theory]
    [InlineData("½ cucharadita de sal", 0.5)]
    [InlineData("¼ taza de aceite", 0.25)]
    [InlineData("¾ kg de tomate", 0.75)]
    [InlineData("⅓ taza de leche", 1.0 / 3.0)]
    public void LasFraccionesDeUnCaracter_SeEntienden(string texto, double esperada)
    {
        var linea = AnalizadorDeIngrediente.Analizar(texto);

        Assert.NotNull(linea);
        Assert.NotNull(linea.Cantidad);
        Assert.Equal((decimal)esperada, linea.Cantidad.Value, precision: 3);
    }

    /// <summary>
    /// Buena parte de las webs de recetas están en inglés. Sin esto, la unidad se
    /// queda pegada al nombre del ingrediente: "tablespoons lime juice".
    /// </summary>
    [Theory]
    [InlineData("3 tablespoons lime juice", 3, Unidad.Cucharada, "lime juice")]
    [InlineData("1 teaspoon kosher salt", 1, Unidad.Cucharadita, "kosher salt")]
    [InlineData("2 cups flour", 2, Unidad.Taza, "flour")]
    [InlineData("2 cloves garlic", 2, Unidad.Diente, "garlic")]
    [InlineData("1/2 cup white onion", 0.5, Unidad.Taza, "white onion")]
    public void LasUnidadesInglesas_SeReconocen(
        string texto, decimal cantidad, Unidad unidad, string nombre)
    {
        var linea = AnalizadorDeIngrediente.Analizar(texto);

        Assert.Equal(cantidad, linea!.Cantidad);
        Assert.Equal(unidad, linea.Unidad);
        Assert.Equal(nombre, linea.Nombre);
    }

    /// <summary>De un rango se toma el número bajo: quedarse corto es más útil.</summary>
    [Fact]
    public void UnRango_TomaElPrimerNumero()
    {
        var linea = AnalizadorDeIngrediente.Analizar("2-3 tomates");

        Assert.Equal(2m, linea!.Cantidad);
        Assert.Equal("tomates", linea.Nombre);
    }

    [Fact]
    public void ElTextoEntreParentesis_SeQuita()
    {
        var linea = AnalizadorDeIngrediente.Analizar("300 g de harina (de fuerza)");

        Assert.Equal("harina", linea!.Nombre);
    }

    [Fact]
    public void LasAbreviaturasConPunto_SeEntienden()
    {
        var linea = AnalizadorDeIngrediente.Analizar("200 gr. de azúcar");

        Assert.Equal(Unidad.Gramo, linea!.Unidad);
        Assert.Equal("azúcar", linea.Nombre);
    }

    // ------------------------------------------------------- No perder nada

    /// <summary>
    /// La regla que gobierna el analizador: una línea que no se entiende se
    /// conserva entera. Descartarla dejaría una receta incompleta sin decirlo.
    /// </summary>
    [Theory]
    [InlineData("sal al gusto")]
    [InlineData("un chorrito de aceite de oliva virgen extra")]
    [InlineData("pimienta negra recién molida")]
    public void UnaLineaQueNoSeEntiende_SeConservaEntera(string texto)
    {
        var linea = AnalizadorDeIngrediente.Analizar(texto);

        Assert.NotNull(linea);
        Assert.Equal(texto, linea.Nombre);
        Assert.Null(linea.Cantidad);
        Assert.Equal(Unidad.AlGusto, linea.Unidad);
    }

    [Fact]
    public void UnaLineaSinIngrediente_SeConservaEntera()
    {
        // "2 unidades" tiene número y unidad, pero ningún ingrediente.
        var linea = AnalizadorDeIngrediente.Analizar("2 unidades");

        Assert.Equal("2 unidades", linea!.Nombre);
        Assert.Null(linea.Cantidad);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinTexto_NoHayLinea(string? texto)
    {
        Assert.Null(AnalizadorDeIngrediente.Analizar(texto));
    }

    [Fact]
    public void UnNombreLarguisimo_SeRecortaEnVezDeFallar()
    {
        var largo = new string('a', 200);

        var linea = AnalizadorDeIngrediente.Analizar($"200 g de {largo}");

        Assert.NotNull(linea);
        Assert.True(linea.Nombre.Length <= 80, $"El nombre mide {linea.Nombre.Length}.");
    }

    /// <summary>
    /// Una pizca no lleva decimales en el dominio: "0,5 pizcas" no significa nada.
    /// </summary>
    [Fact]
    public void UnaPizca_QuedaConCantidadEntera()
    {
        var linea = AnalizadorDeIngrediente.Analizar("1 pizca de sal");

        Assert.Equal(Unidad.Pizca, linea!.Unidad);
        Assert.Equal(1m, linea.Cantidad);
    }

    /// <summary>
    /// Lo que produce el analizador tiene que poder entrar en una receta: si el
    /// dominio lo rechazara, la importación fallaría al guardar y no al leer.
    /// </summary>
    [Theory]
    [InlineData("300 g de harina")]
    [InlineData("sal al gusto")]
    [InlineData("2 huevos")]
    [InlineData("1 pizca de canela")]
    public void LoQueDevuelve_LoAceptaElDominio(string texto)
    {
        var linea = AnalizadorDeIngrediente.Analizar(texto);

        var creada = IngredienteDeReceta.Crear(Guid.NewGuid(), Guid.NewGuid(), linea!.Cantidad, linea.Unidad);

        Assert.NotNull(creada);
    }
}
