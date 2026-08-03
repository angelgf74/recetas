using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Recetas;

public class GestionDeRecetasTests
{
    private readonly RepositorioDeRecetasEnMemoria _recetas = new();
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly AlmacenDeFotosEnMemoria _fotos = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bruno = Guid.NewGuid();

    private GestionDeRecetas Gestion =>
        new(_recetas, new ResolverIngredientes(_ingredientes), _fotos, _reloj);

    private static DatosDeReceta Datos(
        string nombre = "Tortilla de patatas",
        params LineaDeIngrediente[] ingredientes) =>
        new(
            nombre,
            TipoDePlato.PlatoPrincipal,
            "Batir los huevos, freír la patata, cuajar.",
            ingredientes.Length > 0
                ? ingredientes
                : [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]);

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_GuardaLaRecetaComoPrivadaYConSuAutor()
    {
        var (resultado, receta) = await Gestion.CrearAsync(_ana, Datos());

        Assert.Equal(ResultadoDeReceta.Correcto, resultado);
        Assert.NotNull(receta);
        Assert.Equal(_ana, receta.AutorId);
        Assert.Equal(Visibilidad.Privada, receta.Visibilidad);
        Assert.Single(receta.Ingredientes);
    }

    [Fact]
    public async Task Crear_RechazaRecetaSinIngredientes()
    {
        var datos = new DatosDeReceta("Algo", TipoDePlato.Postre, "Pasos", []);

        var (resultado, receta) = await Gestion.CrearAsync(_ana, datos);

        Assert.Equal(ResultadoDeReceta.DatosNoValidos, resultado);
        Assert.Null(receta);
        Assert.Empty(_recetas.Todas);
    }

    [Fact]
    public async Task Crear_RechazaElMismoIngredienteRepetido()
    {
        var datos = Datos(
            ingredientes:
            [
                new LineaDeIngrediente("Tomate", 2m, Unidad.Unidad),
                // El mismo ingrediente escrito distinto: tras normalizar es uno solo.
                new LineaDeIngrediente("  TOMATE ", 3m, Unidad.Unidad)
            ]);

        var (resultado, _) = await Gestion.CrearAsync(_ana, datos);

        Assert.Equal(ResultadoDeReceta.DatosNoValidos, resultado);
    }

    [Fact]
    public async Task Crear_ReutilizaLosIngredientesDelCatalogo()
    {
        var gestion = Gestion;

        await gestion.CrearAsync(_ana, Datos("Una", new LineaDeIngrediente("Tomate", 2m, Unidad.Unidad)));
        await gestion.CrearAsync(_bruno, Datos("Otra", new LineaDeIngrediente("  tomate  ", 5m, Unidad.Unidad)));

        // Dos recetas de dos usuarios, un único "tomate" en el catálogo: es lo que
        // hará posible la búsqueda por ingrediente de la feature 006.
        Assert.Equal(1, _ingredientes.Total);
    }

    [Fact]
    public async Task Crear_RechazaCantidadNoPositivaConUnidadReal()
    {
        var datos = Datos(ingredientes: [new LineaDeIngrediente("Patata", 0m, Unidad.Gramo)]);

        var (resultado, _) = await Gestion.CrearAsync(_ana, datos);

        Assert.Equal(ResultadoDeReceta.DatosNoValidos, resultado);
    }

    [Fact]
    public async Task Crear_AceptaAlGustoSinCantidad()
    {
        var datos = Datos(ingredientes: [new LineaDeIngrediente("Sal", null, Unidad.AlGusto)]);

        var (resultado, receta) = await Gestion.CrearAsync(_ana, datos);

        Assert.Equal(ResultadoDeReceta.Correcto, resultado);
        Assert.Null(receta!.Ingredientes.Single().Cantidad);
    }

    // ------------------------------------------------------------- Aislamiento

    [Fact]
    public async Task Obtener_DevuelveLaPropia()
    {
        var gestion = Gestion;
        var (_, creada) = await gestion.CrearAsync(_ana, Datos());

        var (resultado, receta) = await gestion.ObtenerAsync(_ana, creada!.Id);

        Assert.Equal(ResultadoDeReceta.Correcto, resultado);
        Assert.Equal(creada.Id, receta!.Id);
    }

    [Fact]
    public async Task Obtener_NoDevuelveLaDeOtroUsuario()
    {
        var gestion = Gestion;
        var (_, deAna) = await gestion.CrearAsync(_ana, Datos());

        var (resultado, receta) = await gestion.ObtenerAsync(_bruno, deAna!.Id);

        Assert.Equal(ResultadoDeReceta.NoEncontrada, resultado);
        Assert.Null(receta);
    }

    [Fact]
    public async Task Obtener_RecetaAjenaYRecetaInexistente_SonIndistinguibles()
    {
        var gestion = Gestion;
        var (_, deAna) = await gestion.CrearAsync(_ana, Datos());

        var ajena = await gestion.ObtenerAsync(_bruno, deAna!.Id);
        var inexistente = await gestion.ObtenerAsync(_bruno, Guid.NewGuid());

        // Distinguirlas permitiría sondear qué identificadores existen de verdad.
        Assert.Equal(ajena.Resultado, inexistente.Resultado);
    }

    [Fact]
    public async Task Listar_DevuelveSoloLasDelUsuario()
    {
        var gestion = Gestion;
        await gestion.CrearAsync(_ana, Datos("Una de Ana"));
        await gestion.CrearAsync(_ana, Datos("Otra de Ana"));
        await gestion.CrearAsync(_bruno, Datos("De Bruno"));

        var deAna = await gestion.ListarMiasAsync(_ana);

        Assert.Equal(2, deAna.Count);
        Assert.All(deAna, receta => Assert.Equal(_ana, receta.AutorId));
    }

    // -------------------------------------------------------------- Actualizar

    [Fact]
    public async Task Actualizar_CambiaLosDatosYLosIngredientes()
    {
        var gestion = Gestion;
        var (_, creada) = await gestion.CrearAsync(_ana, Datos());

        _reloj.Avanzar(TimeSpan.FromHours(3));

        var resultado = await gestion.ActualizarAsync(_ana, creada!.Id, new DatosDeReceta(
            "Tortilla con cebolla",
            TipoDePlato.Entrante,
            "Pasos nuevos",
            [new LineaDeIngrediente("Cebolla", 1m, Unidad.Unidad)]));

        Assert.Equal(ResultadoDeReceta.Correcto, resultado);
        Assert.Equal("Tortilla con cebolla", creada.Nombre);
        Assert.Equal(TipoDePlato.Entrante, creada.TipoDePlato);
        Assert.Single(creada.Ingredientes);
    }

    [Fact]
    public async Task Actualizar_NoTocaLaRecetaDeOtroUsuario()
    {
        var gestion = Gestion;
        var (_, deAna) = await gestion.CrearAsync(_ana, Datos("Original"));

        var resultado = await gestion.ActualizarAsync(_bruno, deAna!.Id, Datos("Secuestrada"));

        Assert.Equal(ResultadoDeReceta.NoEncontrada, resultado);
        Assert.Equal("Original", deAna.Nombre);
    }

    // ------------------------------------------------------------------ Borrar

    [Fact]
    public async Task Borrar_EliminaLaPropia()
    {
        var gestion = Gestion;
        var (_, creada) = await gestion.CrearAsync(_ana, Datos());

        var resultado = await gestion.BorrarAsync(_ana, creada!.Id);

        Assert.Equal(ResultadoDeReceta.Correcto, resultado);
        Assert.Empty(_recetas.Todas);
    }

    [Fact]
    public async Task Borrar_NoEliminaLaDeOtroUsuario()
    {
        var gestion = Gestion;
        var (_, deAna) = await gestion.CrearAsync(_ana, Datos());

        var resultado = await gestion.BorrarAsync(_bruno, deAna!.Id);

        Assert.Equal(ResultadoDeReceta.NoEncontrada, resultado);
        Assert.Single(_recetas.Todas);
    }
}
