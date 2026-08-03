using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class RecetaTests
{
    private static readonly DateTimeOffset Momento = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Autor = Guid.NewGuid();

    private static Receta Crear() =>
        Receta.Crear(Autor, "Tortilla de patatas", TipoDePlato.PlatoPrincipal, "Batir, freír, cuajar.", Momento);

    [Fact]
    public void ReciénCreada_EsPrivada()
    {
        // La visibilidad no es un parámetro: no hay forma de pedir que nazca pública.
        Assert.Equal(Visibilidad.Privada, Crear().Visibilidad);
    }

    [Fact]
    public void EsDe_SoloReconoceASuAutor()
    {
        var receta = Crear();

        Assert.True(receta.EsDe(Autor));
        Assert.False(receta.EsDe(Guid.NewGuid()));
    }

    [Fact]
    public void Crear_ExigeAutor()
    {
        Assert.Throws<ArgumentException>(() =>
            Receta.Crear(Guid.Empty, "Algo", TipoDePlato.Postre, "Pasos", Momento));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_RechazaNombreVacio(string nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            Receta.Crear(Autor, nombre, TipoDePlato.Postre, "Pasos", Momento));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_RechazaElaboracionVacia(string elaboracion)
    {
        Assert.Throws<ArgumentException>(() =>
            Receta.Crear(Autor, "Algo", TipoDePlato.Postre, elaboracion, Momento));
    }

    [Fact]
    public void Actualizar_NoCambiaAutorNiVisibilidad()
    {
        var receta = Crear();

        receta.Actualizar("Otro nombre", TipoDePlato.Postre, "Otros pasos", Momento.AddHours(1));

        Assert.Equal("Otro nombre", receta.Nombre);
        Assert.Equal(TipoDePlato.Postre, receta.TipoDePlato);
        // Publicar es cosa de la feature 005; editar no puede colarlo.
        Assert.Equal(Visibilidad.Privada, receta.Visibilidad);
        Assert.Equal(Autor, receta.AutorId);
    }

    [Fact]
    public void Actualizar_MueveLaFechaDeModificacion()
    {
        var receta = Crear();

        receta.Actualizar("Otro", TipoDePlato.Postre, "Pasos", Momento.AddHours(2));

        Assert.Equal(Momento, receta.FechaDeCreacion);
        Assert.Equal(Momento.AddHours(2), receta.FechaDeModificacion);
    }

    [Fact]
    public void ReemplazarIngredientes_SustituyeLaListaEntera()
    {
        var receta = Crear();
        var patata = Guid.NewGuid();
        var huevo = Guid.NewGuid();
        var cebolla = Guid.NewGuid();

        receta.ReemplazarIngredientes([(patata, 500m, Unidad.Gramo), (huevo, 4m, Unidad.Unidad)]);
        receta.ReemplazarIngredientes([(patata, 600m, Unidad.Gramo), (cebolla, 1m, Unidad.Unidad)]);

        Assert.Equal(2, receta.Ingredientes.Count);
        Assert.DoesNotContain(receta.Ingredientes, linea => linea.IngredienteId == huevo);
        Assert.Equal(600m, receta.Ingredientes.Single(linea => linea.IngredienteId == patata).Cantidad);
    }

    [Fact]
    public void ReemplazarIngredientes_RechazaListaVacia()
    {
        Assert.Throws<ArgumentException>(() => Crear().ReemplazarIngredientes([]));
    }

    [Fact]
    public void ReemplazarIngredientes_RechazaElMismoIngredienteDosVeces()
    {
        var receta = Crear();
        var patata = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            receta.ReemplazarIngredientes([(patata, 1m, Unidad.Gramo), (patata, 2m, Unidad.Gramo)]));
    }

    [Fact]
    public void AlGusto_NoGuardaCantidad()
    {
        var receta = Crear();

        // Aunque llegue un número, "al gusto" no tiene cantidad: se normaliza en
        // lugar de rechazar, porque es una incoherencia del cliente y no un motivo
        // para impedir guardar la receta.
        receta.ReemplazarIngredientes([(Guid.NewGuid(), 5m, Unidad.AlGusto)]);

        Assert.Null(receta.Ingredientes.Single().Cantidad);
    }

    [Theory]
    [InlineData(null)]
    // Con sufijo decimal: xUnit no convierte un literal entero a double?.
    [InlineData(0d)]
    [InlineData(-1d)]
    public void CantidadNoPositiva_ConUnidadReal_EsRechazada(double? cantidad)
    {
        var receta = Crear();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            receta.ReemplazarIngredientes([(Guid.NewGuid(), (decimal?)cantidad, Unidad.Gramo)]));
    }
}
