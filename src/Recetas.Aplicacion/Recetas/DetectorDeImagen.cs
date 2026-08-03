using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <summary>
/// Determina el formato de una imagen leyendo sus primeros bytes.
/// </summary>
/// <remarks>
/// Deliberadamente ignora el <c>Content-Type</c> y la extensión que envíe el
/// cliente: los dos los controla quien sube el archivo. Con la cabecera real, un
/// script disfrazado de <c>.jpg</c> se rechaza en la puerta en lugar de acabar
/// guardado y servido desde el dominio de la API.
/// </remarks>
public static class DetectorDeImagen
{
    /// <summary>Bytes que hay que leer para reconocer cualquiera de los formatos.</summary>
    public const int BytesNecesarios = 12;

    public static bool TryDetectar(ReadOnlySpan<byte> cabecera, out TipoDeImagen tipo)
    {
        tipo = default;

        if (cabecera.Length < 3)
        {
            return false;
        }

        // JPEG: FF D8 FF
        if (cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
        {
            tipo = TipoDeImagen.Jpeg;
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (cabecera.Length >= 8
            && cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47
            && cabecera[4] == 0x0D && cabecera[5] == 0x0A && cabecera[6] == 0x1A && cabecera[7] == 0x0A)
        {
            tipo = TipoDeImagen.Png;
            return true;
        }

        // WebP: "RIFF" .... "WEBP" — el tamaño va en medio, por eso se comprueban
        // dos tramos separados y hacen falta 12 bytes.
        if (cabecera.Length >= 12
            && cabecera[0] == (byte)'R' && cabecera[1] == (byte)'I'
            && cabecera[2] == (byte)'F' && cabecera[3] == (byte)'F'
            && cabecera[8] == (byte)'W' && cabecera[9] == (byte)'E'
            && cabecera[10] == (byte)'B' && cabecera[11] == (byte)'P')
        {
            tipo = TipoDeImagen.Webp;
            return true;
        }

        return false;
    }
}
