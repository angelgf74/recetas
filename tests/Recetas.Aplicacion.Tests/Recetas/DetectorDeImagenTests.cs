using Recetas.Aplicacion.Recetas;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Recetas;

public class DetectorDeImagenTests
{
    public static byte[] Jpeg() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    public static byte[] Webp() =>
        [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x24, 0x00, 0x00, 0x00,
         (byte)'W', (byte)'E', (byte)'B', (byte)'P'];

    [Fact]
    public void Detecta_Jpeg() => Assert.True(Detectado(Jpeg(), TipoDeImagen.Jpeg));

    [Fact]
    public void Detecta_Png() => Assert.True(Detectado(Png(), TipoDeImagen.Png));

    [Fact]
    public void Detecta_Webp() => Assert.True(Detectado(Webp(), TipoDeImagen.Webp));

    [Fact]
    public void Rechaza_UnScriptDisfrazadoDeImagen()
    {
        // El caso que motiva detectar por contenido: el cliente puede llamarlo
        // "foto.jpg" y declararlo como image/jpeg, pero los bytes son HTML.
        var html = "<html><script>alert(1)</script></html>"u8.ToArray();

        Assert.False(DetectorDeImagen.TryDetectar(html, out _));
    }

    [Fact]
    public void Rechaza_UnPdf()
    {
        Assert.False(DetectorDeImagen.TryDetectar("%PDF-1.7"u8.ToArray(), out _));
    }

    [Fact]
    public void Rechaza_ContenidoVacio()
    {
        Assert.False(DetectorDeImagen.TryDetectar([], out _));
    }

    [Fact]
    public void Rechaza_UnRiffQueNoEsWebp()
    {
        // Un WAV también empieza por "RIFF": solo la marca "WEBP" del byte 8 lo
        // distingue, y por eso hacen falta doce bytes y no cuatro.
        byte[] wav =
        [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x24, 0x00, 0x00, 0x00,
            (byte)'W', (byte)'A', (byte)'V', (byte)'E'
        ];

        Assert.False(DetectorDeImagen.TryDetectar(wav, out _));
    }

    private static bool Detectado(byte[] bytes, TipoDeImagen esperado) =>
        DetectorDeImagen.TryDetectar(bytes, out var tipo) && tipo == esperado;
}
