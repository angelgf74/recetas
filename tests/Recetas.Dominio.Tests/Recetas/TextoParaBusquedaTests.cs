using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class TextoParaBusquedaTests
{
    [Theory]
    [InlineData("Tortilla", "tortilla")]
    [InlineData("TORTILLA", "tortilla")]
    [InlineData("  Tortilla  ", "tortilla")]
    [InlineData("Tortilla   de    patatas", "tortilla de patatas")]
    public void Normaliza_MayusculasYEspacios(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoParaBusqueda.Normalizar(entrada));
    }

    [Theory]
    [InlineData("Jamón", "jamon")]
    [InlineData("Pimentón dulce", "pimenton dulce")]
    [InlineData("Anís", "anis")]
    [InlineData("Crème brûlée", "creme brulee")]
    public void Normaliza_QuitaLosAcentos(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoParaBusqueda.Normalizar(entrada));
    }

    [Theory]
    [InlineData("Piña", "pina")]
    [InlineData("Jalapeño", "jalapeno")]
    public void Normaliza_TambienLaEne(string entrada, string esperado)
    {
        // Decisión consciente: se escribe "pina colada" mucho más a menudo de lo
        // que se teclea la ñ, y aquí la prioridad es encontrar. El nombre que se
        // muestra conserva la ñ.
        Assert.Equal(esperado, TextoParaBusqueda.Normalizar(entrada));
    }

    [Fact]
    public void LaConsultaYElDatoGuardado_NormalizanIgual()
    {
        // La propiedad de la que depende todo: si el texto guardado y el de la
        // búsqueda se normalizaran distinto, no casarían nunca y no habría ningún
        // error, solo búsquedas vacías.
        const string comoLoGuardaElUsuario = "Pimentón  DULCE";
        const string comoLoEscribeAlBuscar = "pimenton dulce";

        Assert.Equal(
            TextoParaBusqueda.Normalizar(comoLoGuardaElUsuario),
            TextoParaBusqueda.Normalizar(comoLoEscribeAlBuscar));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normaliza_VaciosADevuelveCadenaVacia(string? entrada)
    {
        Assert.Equal(string.Empty, TextoParaBusqueda.Normalizar(entrada));
    }

    [Fact]
    public void UnaRecetaGuardaSuNombreNormalizado()
    {
        var receta = Receta.Crear(
            Guid.NewGuid(), "Jamón  Asado", TipoDePlato.PlatoPrincipal, "Pasos", DateTimeOffset.UtcNow);

        Assert.Equal("Jamón  Asado", receta.Nombre);
        Assert.Equal("jamon asado", receta.NombreParaBusqueda);
    }

    [Fact]
    public void RenombrarUnaReceta_ActualizaLaFormaDeBusqueda()
    {
        var receta = Receta.Crear(
            Guid.NewGuid(), "Jamón", TipoDePlato.PlatoPrincipal, "Pasos", DateTimeOffset.UtcNow);

        receta.Actualizar("Salmón", TipoDePlato.PlatoPrincipal, "Pasos", DateTimeOffset.UtcNow);

        // Sin recalcular al editar, la receta seguiría encontrándose por su nombre
        // antiguo y no por el nuevo.
        Assert.Equal("salmon", receta.NombreParaBusqueda);
    }

    [Fact]
    public void UnIngredienteGuardaSuNombreNormalizado()
    {
        var ingrediente = Ingrediente.Crear(NombreDeIngrediente.Crear("Pimentón Dulce"));

        Assert.Equal("pimentón dulce", ingrediente.Nombre.Valor);
        Assert.Equal("pimenton dulce", ingrediente.NombreParaBusqueda);
    }
}
