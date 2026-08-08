using Microsoft.Extensions.Logging.Abstractions;
using Recetas.Dominio.Recetas;
using Recetas.Infraestructura.Fotos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Recetas.Infraestructura.Tests.Fotos;

/// <summary>
/// Comprueba, con imágenes reales, que los metadatos desaparecen.
/// </summary>
/// <remarks>
/// Es el test que sostiene el requisito de la feature 005: publicar una receta
/// con la foto sin limpiar expondría la dirección de quien la cocinó.
/// </remarks>
public class LimpiadorDeImagenesTests
{
    private readonly LimpiadorDeImagenesConImageSharp _limpiador =
        new(NullLogger<LimpiadorDeImagenesConImageSharp>.Instance);

    /// <summary>Genera un JPEG con coordenadas GPS incrustadas, como el de un móvil.</summary>
    private static MemoryStream JpegConGps()
    {
        using var imagen = new Image<Rgba32>(40, 20);

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(40), new Rational(24), new Rational(59)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.GPSLongitude, [new Rational(3), new Rational(42), new Rational(9)]);
        exif.SetValue(ExifTag.Make, "Marca del movil");
        imagen.Metadata.ExifProfile = exif;

        var flujo = new MemoryStream();
        imagen.Save(flujo, new JpegEncoder());
        flujo.Position = 0;

        return flujo;
    }

    [Fact]
    public async Task LaImagenDeEntrada_TieneGps()
    {
        // Comprobación del propio montaje: sin esto, el test de abajo podría estar
        // pasando simplemente porque la imagen nunca tuvo coordenadas.
        using var conGps = JpegConGps();
        using var imagen = await Image.LoadAsync(conGps);

        Assert.NotNull(imagen.Metadata.ExifProfile);
        Assert.True(imagen.Metadata.ExifProfile!.TryGetValue(ExifTag.GPSLatitude, out _));
    }

    [Fact]
    public async Task Limpiar_QuitaLasCoordenadasGps()
    {
        using var conGps = JpegConGps();

        using var limpia = await _limpiador.LimpiarAsync(conGps, TipoDeImagen.Jpeg);

        Assert.NotNull(limpia);
        using var resultado = await Image.LoadAsync(limpia!);

        // Ni el perfil entero, ni la etiqueta suelta.
        Assert.True(
            resultado.Metadata.ExifProfile is null
            || !resultado.Metadata.ExifProfile.TryGetValue(ExifTag.GPSLatitude, out _),
            "La imagen limpia conserva coordenadas GPS.");
    }

    [Fact]
    public async Task Limpiar_QuitaTambienElRestoDeMetadatos()
    {
        using var conGps = JpegConGps();

        using var limpia = await _limpiador.LimpiarAsync(conGps, TipoDeImagen.Jpeg);
        using var resultado = await Image.LoadAsync(limpia!);

        Assert.Null(resultado.Metadata.XmpProfile);
        Assert.Null(resultado.Metadata.IptcProfile);
    }

    [Fact]
    public async Task Limpiar_DevuelveUnaImagenValidaDelMismoTamano()
    {
        using var conGps = JpegConGps();

        using var limpia = await _limpiador.LimpiarAsync(conGps, TipoDeImagen.Jpeg);
        using var resultado = await Image.LoadAsync(limpia!);

        Assert.Equal(40, resultado.Width);
        Assert.Equal(20, resultado.Height);
    }

    [Fact]
    public async Task Limpiar_AplicaLaOrientacionAntesDeTirarla()
    {
        // Orientación 6 = girada 90°. El dato vive en el EXIF que se descarta, así
        // que hay que aplicarlo a los píxeles o la foto se guardaría tumbada.
        using var original = new Image<Rgba32>(40, 20);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)6);
        original.Metadata.ExifProfile = exif;

        using var flujo = new MemoryStream();
        original.Save(flujo, new JpegEncoder());
        flujo.Position = 0;

        using var limpia = await _limpiador.LimpiarAsync(flujo, TipoDeImagen.Jpeg);
        using var resultado = await Image.LoadAsync(limpia!);

        // Ancho y alto intercambiados: el giro se aplicó de verdad.
        Assert.Equal(20, resultado.Width);
        Assert.Equal(40, resultado.Height);
    }

    [Fact]
    public async Task Limpiar_RechazaContenidoQueNoSePuedeDecodificar()
    {
        // Cabecera de JPEG válida pero cuerpo basura: el detector lo dejaría pasar
        // y es aquí donde se corta.
        using var basura = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 1, 2, 3, 4, 5, 6, 7, 8]);

        var limpia = await _limpiador.LimpiarAsync(basura, TipoDeImagen.Jpeg);

        Assert.Null(limpia);
    }
}
