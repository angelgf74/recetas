using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

/// <summary>Calco de <see cref="NombreDeIngredienteTests"/>.</summary>
public class NombreDeEtiquetaTests
{
    [Theory]
    [InlineData("Rápido", "rápido")]
    [InlineData("  rápido  ", "rápido")]
    [InlineData("SIN GLUTEN", "sin gluten")]
    [InlineData("sin   gluten", "sin gluten")]
    [InlineData("\tDe la abuela\n", "de la abuela")]
    public void Normaliza_ARepresentacionUnica(string entrada, string esperado)
    {
        Assert.True(NombreDeEtiqueta.TryCrear(entrada, out var nombre));
        Assert.Equal(esperado, nombre.Valor);
    }

    [Fact]
    public void DosEscriturasDeLaMismaEtiqueta_SonIguales()
    {
        var una = NombreDeEtiqueta.Crear("Sin  Gluten");
        var otra = NombreDeEtiqueta.Crear("  sin gluten ");

        Assert.Equal(una, otra);
        Assert.Equal(una.GetHashCode(), otra.GetHashCode());
    }

    [Fact]
    public void NoQuitaLosAcentos()
    {
        Assert.NotEqual(NombreDeEtiqueta.Crear("rápido"), NombreDeEtiqueta.Crear("rapido"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rechaza_VacioONulo(string? entrada)
    {
        Assert.False(NombreDeEtiqueta.TryCrear(entrada, out _));
    }

    [Fact]
    public void Rechaza_NombresDemasiadoLargos()
    {
        Assert.False(NombreDeEtiqueta.TryCrear(new string('a', NombreDeEtiqueta.LongitudMaxima + 1), out _));
    }
}
