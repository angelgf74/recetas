using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

public class FotoDePortadaTests
{
    private static readonly DateTimeOffset Momento = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static Receta Crear() =>
        Receta.Crear(Guid.NewGuid(), "Tortilla", TipoDePlato.PlatoPrincipal, "Pasos.", Momento);

    [Fact]
    public void SinFotos_NoHayPortada()
    {
        Assert.Null(Crear().FotoDePortada);
    }

    [Fact]
    public void ConUnaFoto_EsaEsLaPortada()
    {
        var receta = Crear();
        var foto = receta.AnadirFoto(TipoDeImagen.Jpeg, 1000, Momento);

        Assert.Equal(foto.Id, receta.FotoDePortada!.Id);
    }

    [Fact]
    public void ConVarias_LaPortadaEsLaMasAntigua()
    {
        var receta = Crear();
        var primera = receta.AnadirFoto(TipoDeImagen.Jpeg, 1000, Momento);
        receta.AnadirFoto(TipoDeImagen.Png, 2000, Momento.AddMinutes(5));
        receta.AnadirFoto(TipoDeImagen.Webp, 3000, Momento.AddMinutes(10));

        Assert.Equal(primera.Id, receta.FotoDePortada!.Id);
    }

    /// <summary>
    /// La portada es derivada, no un campo: borrar la que hacía de portada debe
    /// dejar de inmediato a la siguiente en su lugar. Con un identificador
    /// guardado, olvidarse de actualizarlo aquí dejaría el listado pidiendo una
    /// foto que ya no existe.
    /// </summary>
    [Fact]
    public void AlBorrarLaPortada_PasaASerloLaSiguiente()
    {
        var receta = Crear();
        var primera = receta.AnadirFoto(TipoDeImagen.Jpeg, 1000, Momento);
        var segunda = receta.AnadirFoto(TipoDeImagen.Jpeg, 2000, Momento.AddMinutes(5));

        receta.QuitarFoto(primera.Id, Momento.AddMinutes(6));

        Assert.Equal(segunda.Id, receta.FotoDePortada!.Id);
    }

    [Fact]
    public void AlBorrarLaUltima_VuelveAQuedarseSinPortada()
    {
        var receta = Crear();
        var foto = receta.AnadirFoto(TipoDeImagen.Jpeg, 1000, Momento);

        receta.QuitarFoto(foto.Id, Momento.AddMinutes(1));

        Assert.Null(receta.FotoDePortada);
    }

    /// <summary>
    /// Dos fotos subidas en el mismo instante —posible al cargar recetas por
    /// script— no pueden dar una portada distinta en cada consulta.
    /// </summary>
    [Fact]
    public void ConLaMismaFecha_LaPortadaEsEstable()
    {
        var receta = Crear();
        receta.AnadirFoto(TipoDeImagen.Jpeg, 1000, Momento);
        receta.AnadirFoto(TipoDeImagen.Jpeg, 2000, Momento);

        var primera = receta.FotoDePortada!.Id;

        Assert.Equal(primera, receta.FotoDePortada!.Id);
    }
}
