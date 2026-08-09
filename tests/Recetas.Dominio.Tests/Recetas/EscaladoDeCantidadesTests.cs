using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

/// <summary>
/// El redondeo por unidad es el corazón de la feature 010: multiplicar lo hace
/// cualquiera, y "1,3333 huevos" es peor que no escalar.
/// </summary>
public class EscaladoDeCantidadesTests
{
    // ------------------------------------------------------- Lo que no escala

    [Fact]
    public void AlGusto_SigueSinCantidad()
    {
        Assert.Null(EscaladoDeCantidades.Escalar(null, Unidad.AlGusto, 4m));
    }

    [Fact]
    public void Pizca_NoSeMultiplica()
    {
        // "Cuatro pizcas" no es una medida: es una pizca echada cuatro veces.
        Assert.Equal(1m, EscaladoDeCantidades.Escalar(1m, Unidad.Pizca, 4m));
    }

    [Theory]
    [InlineData(Unidad.AlGusto)]
    [InlineData(Unidad.Pizca)]
    public void LasUnidadesQueNoEscalan_LoDicen(Unidad unidad)
    {
        Assert.False(EscaladoDeCantidades.SeEscala(unidad));
    }

    // ------------------------------------------------------------- Contables

    [Theory]
    [InlineData(Unidad.Unidad)]
    [InlineData(Unidad.Diente)]
    [InlineData(Unidad.Rama)]
    [InlineData(Unidad.Hoja)]
    public void LosContables_SeRedondeanAMediaUnidad(Unidad unidad)
    {
        // 2 × 1,333… = 2,666… → 2,5. Media cebolla es una cantidad real.
        Assert.Equal(2.5m, EscaladoDeCantidades.Escalar(2m, unidad, 4m / 3m));
    }

    [Fact]
    public void UnContable_QueSaleExacto_NoSeToca()
    {
        Assert.Equal(4m, EscaladoDeCantidades.Escalar(2m, Unidad.Unidad, 2m));
    }

    // -------------------------------------------------------- Peso y volumen

    [Theory]
    [InlineData(Unidad.Gramo)]
    [InlineData(Unidad.Mililitro)]
    public void GramosYMililitros_SeRedondeanAEntero(Unidad unidad)
    {
        // 100 ÷ 3 = 33,333… → 33. La báscula no mide décimas de gramo.
        Assert.Equal(33m, EscaladoDeCantidades.Escalar(100m, unidad, 1m / 3m));
    }

    // ------------------------------------------------------------- El resto

    [Theory]
    [InlineData(Unidad.Cucharada)]
    [InlineData(Unidad.Cucharadita)]
    [InlineData(Unidad.Taza)]
    [InlineData(Unidad.Kilogramo)]
    [InlineData(Unidad.Litro)]
    public void ElResto_SeRedondeaACuartos(Unidad unidad)
    {
        // 1 ÷ 3 = 0,333… → 0,25. Un cuarto de cucharadita se mide; 0,333 no.
        Assert.Equal(0.25m, EscaladoDeCantidades.Escalar(1m, unidad, 1m / 3m));
    }

    // ----------------------------------------------------------- Nunca cero

    [Fact]
    public void UnaCantidadDiminuta_NoDesaparece()
    {
        // 1 ÷ 8 = 0,125, que redondeado a cuartos sería 0 y borraría el
        // ingrediente de la receta. Un poco de más es mejor que nada.
        Assert.Equal(0.25m, EscaladoDeCantidades.Escalar(1m, Unidad.Cucharadita, 0.125m));
    }

    [Fact]
    public void UnGramoDividido_NoDesaparece()
    {
        Assert.Equal(1m, EscaladoDeCantidades.Escalar(1m, Unidad.Gramo, 0.1m));
    }

    [Fact]
    public void MedioContable_EsElMinimo()
    {
        Assert.Equal(0.5m, EscaladoDeCantidades.Escalar(1m, Unidad.Unidad, 0.1m));
    }

    // ------------------------------------------------------------- Factor 1

    [Theory]
    [InlineData(Unidad.Gramo, 137)]
    [InlineData(Unidad.Cucharadita, 3)]
    [InlineData(Unidad.Unidad, 2)]
    public void ConFactorUno_LaCantidadNoCambia(Unidad unidad, int cantidad)
    {
        Assert.Equal(cantidad, EscaladoDeCantidades.Escalar(cantidad, unidad, 1m));
    }

    [Fact]
    public void UnFactorNoPositivo_EsUnError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EscaladoDeCantidades.Escalar(100m, Unidad.Gramo, 0m));
    }

    // ------------------------------------------- Conversión de unidad (026)

    [Fact]
    public void Gramos_AlLlegarAMil_ConvierteAKilogramos()
    {
        var (cantidad, unidad) = EscaladoDeCantidades.EscalarConUnidad(500m, Unidad.Gramo, 2m);

        Assert.Equal(Unidad.Kilogramo, unidad);
        Assert.Equal(1m, cantidad);
    }

    [Fact]
    public void Mililitros_AlLlegarAMil_ConvierteALitros()
    {
        var (cantidad, unidad) = EscaladoDeCantidades.EscalarConUnidad(250m, Unidad.Mililitro, 4m);

        Assert.Equal(Unidad.Litro, unidad);
        Assert.Equal(1m, cantidad);
    }

    [Fact]
    public void PorDebajoDeMil_SigueEnGramos()
    {
        var (cantidad, unidad) = EscaladoDeCantidades.EscalarConUnidad(500m, Unidad.Gramo, 1.5m);

        Assert.Equal(Unidad.Gramo, unidad);
        Assert.Equal(750m, cantidad);
    }

    [Fact]
    public void LaConversion_RedondeaUnaSolaVezEnLaUnidadDeDestino()
    {
        // 900 × 2 = 1800 g → 1,8 kg exactos antes de redondear; el kilogramo
        // redondea a cuartos, así que el resultado final es 1,75 kg y no un
        // redondeo distinto que arrastrara el paso intermedio en gramos.
        var (cantidad, unidad) = EscaladoDeCantidades.EscalarConUnidad(900m, Unidad.Gramo, 2m);

        Assert.Equal(Unidad.Kilogramo, unidad);
        Assert.Equal(1.75m, cantidad);
    }

    [Theory]
    [InlineData(Unidad.Cucharada)]
    [InlineData(Unidad.Cucharadita)]
    [InlineData(Unidad.Taza)]
    [InlineData(Unidad.Unidad)]
    [InlineData(Unidad.Kilogramo)]
    [InlineData(Unidad.Litro)]
    public void UnidadesSinPareja_NuncaCambianDeUnidad(Unidad unidad)
    {
        var (_, unidadFinal) = EscaladoDeCantidades.EscalarConUnidad(500m, unidad, 10m);

        Assert.Equal(unidad, unidadFinal);
    }

    [Fact]
    public void AlGusto_NoConvierteNiCambia()
    {
        var (cantidad, unidad) = EscaladoDeCantidades.EscalarConUnidad(null, Unidad.AlGusto, 4m);

        Assert.Null(cantidad);
        Assert.Equal(Unidad.AlGusto, unidad);
    }
}
