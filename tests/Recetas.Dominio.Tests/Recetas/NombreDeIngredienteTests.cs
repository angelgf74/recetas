using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class NombreDeIngredienteTests
{
    [Theory]
    [InlineData("Tomate", "tomate")]
    [InlineData("  tomate  ", "tomate")]
    [InlineData("ACEITE DE OLIVA", "aceite de oliva")]
    [InlineData("aceite   de    oliva", "aceite de oliva")]
    [InlineData("\tPimienta\n", "pimienta")]
    public void Normaliza_ARepresentacionUnica(string entrada, string esperado)
    {
        Assert.True(NombreDeIngrediente.TryCrear(entrada, out var nombre));
        Assert.Equal(esperado, nombre.Valor);
    }

    [Fact]
    public void DosEscriturasDelMismoIngrediente_SonIguales()
    {
        var uno = NombreDeIngrediente.Crear("Aceite  De   Oliva");
        var otro = NombreDeIngrediente.Crear("  aceite de oliva ");

        // De esto depende que el catálogo no se duplique y que la búsqueda por
        // ingredientes de la feature 006 devuelva resultados completos.
        Assert.Equal(uno, otro);
        Assert.Equal(uno.GetHashCode(), otro.GetHashCode());
    }

    [Fact]
    public void NoQuitaLosAcentos()
    {
        // En español distinguen palabras: "anís" no debe colapsar con "anis".
        Assert.NotEqual(NombreDeIngrediente.Crear("anís"), NombreDeIngrediente.Crear("anis"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rechaza_VacioONulo(string? entrada)
    {
        Assert.False(NombreDeIngrediente.TryCrear(entrada, out _));
    }

    [Fact]
    public void Rechaza_NombresDemasiadoLargos()
    {
        Assert.False(NombreDeIngrediente.TryCrear(new string('a', NombreDeIngrediente.LongitudMaxima + 1), out _));
    }
}
