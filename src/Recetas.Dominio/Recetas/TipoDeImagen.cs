namespace Recetas.Dominio.Recetas;

/// <summary>
/// Formatos de imagen admitidos.
/// </summary>
/// <remarks>
/// El tipo <b>siempre</b> se deduce de los bytes del archivo, nunca del
/// <c>Content-Type</c> que declare el cliente. Fiarse de lo declarado permitiría
/// subir cualquier cosa etiquetada como imagen y luego servirla con ese tipo, que
/// es como se acaba sirviendo HTML —y por tanto JavaScript— desde el dominio de la
/// API.
/// </remarks>
public enum TipoDeImagen
{
    Jpeg,
    Png,
    Webp
}

public static class TipoDeImagenExtensiones
{
    /// <summary>Tipo de contenido con el que se sirve la foto.</summary>
    public static string TipoDeContenido(this TipoDeImagen tipo) => tipo switch
    {
        TipoDeImagen.Jpeg => "image/jpeg",
        TipoDeImagen.Png => "image/png",
        TipoDeImagen.Webp => "image/webp",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    /// <summary>Extensión del archivo en disco. La usa el almacén, no el usuario.</summary>
    public static string Extension(this TipoDeImagen tipo) => tipo switch
    {
        TipoDeImagen.Jpeg => "jpg",
        TipoDeImagen.Png => "png",
        TipoDeImagen.Webp => "webp",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };
}
