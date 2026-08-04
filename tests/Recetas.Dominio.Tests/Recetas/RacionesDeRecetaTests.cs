using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class RacionesDeRecetaTests
{
    private static readonly DateTimeOffset Momento = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Harina = Guid.NewGuid();
    private static readonly Guid Huevos = Guid.NewGuid();
    private static readonly Guid Sal = Guid.NewGuid();

    /// <summary>Receta para 4: 300 g de harina, 2 huevos y sal al gusto.</summary>
    private static Receta Crear(int? raciones = 4)
    {
        var receta = Receta.Crear(
            Guid.NewGuid(), "Tortitas", TipoDePlato.Postre, "Mezclar y cuajar.", Momento, raciones);

        receta.ReemplazarIngredientes(
        [
            (Harina, 300m, Unidad.Gramo),
            (Huevos, 2m, Unidad.Unidad),
            (Sal, null, Unidad.AlGusto)
        ]);

        return receta;
    }

    private static decimal? CantidadDe(IReadOnlyList<LineaEscalada> lineas, Guid ingrediente) =>
        lineas.Single(linea => linea.IngredienteId == ingrediente).Cantidad;

    // --------------------------------------------------------------- Raciones

    [Fact]
    public void SinIndicarlas_LaRecetaNoTieneRaciones()
    {
        Assert.Null(Crear(raciones: null).Raciones);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void FueraDeRango_NoSePuedeCrear(int raciones)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Crear(raciones));
    }

    [Fact]
    public void Actualizar_PuedeQuitarlas()
    {
        var receta = Crear();

        // Quitarlas es una edición legítima: "resulta que no sé para cuántos es".
        receta.Actualizar("Tortitas", TipoDePlato.Postre, "Mezclar.", Momento, raciones: null);

        Assert.Null(receta.Raciones);
    }

    // --------------------------------------------------------------- Escalar

    [Fact]
    public void Doblar_DoblaLasCantidades()
    {
        var lineas = Crear().EscalarA(8);

        Assert.Equal(600m, CantidadDe(lineas, Harina));
        Assert.Equal(4m, CantidadDe(lineas, Huevos));
    }

    [Fact]
    public void Reducir_ReduceLasCantidades()
    {
        var lineas = Crear().EscalarA(2);

        Assert.Equal(150m, CantidadDe(lineas, Harina));
        Assert.Equal(1m, CantidadDe(lineas, Huevos));
    }

    [Fact]
    public void LasMismasRaciones_DevuelvenLasCantidadesExactas()
    {
        var lineas = Crear().EscalarA(4);

        Assert.Equal(300m, CantidadDe(lineas, Harina));
        Assert.Equal(2m, CantidadDe(lineas, Huevos));
    }

    [Fact]
    public void AlGusto_SigueAlGusto()
    {
        Assert.Null(CantidadDe(Crear().EscalarA(12), Sal));
    }

    /// <summary>
    /// Escalar es leer, no editar: si mutara la receta, ajustar y guardar después
    /// una foto persistiría las cantidades ajustadas como si fueran las de verdad.
    /// </summary>
    [Fact]
    public void Escalar_NoTocaLaReceta()
    {
        var receta = Crear();
        var antes = receta.FechaDeModificacion;

        receta.EscalarA(12);

        Assert.Equal(300m, receta.Ingredientes.Single(l => l.IngredienteId == Harina).Cantidad);
        Assert.Equal(4, receta.Raciones);
        Assert.Equal(antes, receta.FechaDeModificacion);
    }

    /// <summary>
    /// Redondear acumula error: escalar desde lo ya escalado daría un resultado
    /// distinto según el camino recorrido.
    /// </summary>
    [Fact]
    public void Escalar_ParteSiempreDeLasCantidadesGuardadas()
    {
        var receta = Crear();

        receta.EscalarA(6);
        var directo = receta.EscalarA(9);

        Assert.Equal(675m, CantidadDe(directo, Harina));
    }

    // ------------------------------------------------- Cuando no se puede

    [Fact]
    public void SinRacionesEnLaReceta_NoSeEscala()
    {
        var lineas = Crear(raciones: null).EscalarA(8);

        Assert.Equal(300m, CantidadDe(lineas, Harina));
    }

    [Fact]
    public void SinPedirRaciones_NoSeEscala()
    {
        Assert.Equal(300m, CantidadDe(Crear().EscalarA(null), Harina));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ConRacionesPedidasFueraDeRango_NoSeEscala(int pedidas)
    {
        // La API responde 400 antes de llegar aquí; el dominio, por si acaso,
        // devuelve las cantidades guardadas en lugar de un factor disparatado.
        Assert.Equal(300m, CantidadDe(Crear().EscalarA(pedidas), Harina));
    }

    // ------------------------------------------------- Raciones mostradas

    [Fact]
    public void RacionesMostradas_SonLasPedidasCuandoSeEscala()
    {
        Assert.Equal(8, Crear().RacionesMostradasPara(8));
    }

    [Fact]
    public void RacionesMostradas_SonLasDeLaRecetaCuandoNoSeEscala()
    {
        Assert.Equal(4, Crear().RacionesMostradasPara(null));
    }

    [Fact]
    public void RacionesMostradas_SonNulasSiLaRecetaNoLasTiene()
    {
        Assert.Null(Crear(raciones: null).RacionesMostradasPara(8));
    }
}
