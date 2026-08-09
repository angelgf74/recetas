using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

public enum ResultadoDeImportacion
{
    Correcto,

    /// <summary>La dirección no es una URL http(s) válida.</summary>
    DireccionNoValida,

    /// <summary>
    /// No se ha podido traer la página. <b>Un solo valor</b> para "no responde",
    /// "no existe", "no es HTML" y "apunta a la red interna": distinguirlos
    /// convertiría el endpoint en un escáner de la red del servidor.
    /// </summary>
    NoSePudoLeer,

    /// <summary>La página se ha traído, pero no publica ninguna receta reconocible.</summary>
    SinReceta
}

/// <summary>
/// Lee una receta de una página web y devuelve un <b>borrador</b>.
/// </summary>
/// <remarks>
/// No toca el repositorio: importar no crea nada. El borrador va al formulario, el
/// usuario lo revisa y es su envío el que guarda la receta. Eso es lo que mantiene
/// la feature dentro de <c>mission.md</c> —"el autor manda sobre sus datos"— y lo
/// que evita llenar recetarios de extracciones malas.
/// </remarks>
public sealed class ImportarReceta(IDescargadorDePaginas descargador, ILimpiadorDeImagenes limpiador)
{
    /// <param name="direccionIndicada">La URL de la página, tal como la escribe el usuario.</param>
    /// <param name="maximoDeBytesDeImagen">
    /// Tope para la foto de la receta (027), si la página declara una. Por
    /// defecto 0: sin presupuesto no se intenta nada, en vez de asumir un límite
    /// que nadie ha decidido. Quien llama —el endpoint— lo resuelve del mismo
    /// límite que rige las fotos que sube el propio usuario.
    /// </param>
    public async Task<(ResultadoDeImportacion Resultado, RecetaImportada? Receta, Uri? Origen)> EjecutarAsync(
        string? direccionIndicada,
        long maximoDeBytesDeImagen = 0,
        CancellationToken cancelacion = default)
    {
        if (!TryLeerDireccion(direccionIndicada, out var direccion))
        {
            return (ResultadoDeImportacion.DireccionNoValida, null, null);
        }

        var html = await descargador.DescargarAsync(direccion, cancelacion);

        if (html is null)
        {
            return (ResultadoDeImportacion.NoSePudoLeer, null, direccion);
        }

        var receta = LectorDeRecetaEnJsonLd.Leer(html);

        if (receta is null)
        {
            return (ResultadoDeImportacion.SinReceta, null, direccion);
        }

        var imagen = await DescargarImagenLimpiaAsync(receta.UrlDeImagen, maximoDeBytesDeImagen, cancelacion);

        return (ResultadoDeImportacion.Correcto, receta with { ImagenLimpia = imagen }, direccion);
    }

    /// <summary>
    /// Trae y limpia la foto de la receta, si la había.
    /// </summary>
    /// <remarks>
    /// Cualquier fallo en el camino —sin URL válida, sin descarga, formato no
    /// admitido, metadatos que no se pueden leer— deja el borrador sin foto: no
    /// aborta una importación que por lo demás ha ido bien. Es la misma filosofía
    /// que ya sigue <c>LectorDeRecetaEnJsonLd</c> con el resto de campos: lo que
    /// no encaja se ignora.
    /// <para>
    /// El formato se decide por los bytes con <c>DetectorDeImagen</c>, y la
    /// limpieza es la misma que las fotos que sube el usuario
    /// (<see cref="ILimpiadorDeImagenes"/>): una foto de un tercero puede llevar
    /// su propio EXIF con ubicación, y no es de quien importa.
    /// </para>
    /// </remarks>
    private async Task<byte[]?> DescargarImagenLimpiaAsync(
        string? urlIndicada, long maximoDeBytes, CancellationToken cancelacion)
    {
        if (maximoDeBytes <= 0 || !TryLeerDireccion(urlIndicada, out var url))
        {
            return null;
        }

        var bytes = await descargador.DescargarImagenAsync(url, maximoDeBytes, cancelacion);

        if (bytes is not { Length: > 0 })
        {
            return null;
        }

        var cabecera = bytes.AsSpan(0, Math.Min(DetectorDeImagen.BytesNecesarios, bytes.Length));

        if (!DetectorDeImagen.TryDetectar(cabecera, out var tipo))
        {
            return null;
        }

        using var origen = new MemoryStream(bytes);
        await using var limpia = await limpiador.LimpiarAsync(origen, tipo, cancelacion);

        if (limpia is null)
        {
            return null;
        }

        using var destino = new MemoryStream();
        await limpia.CopyToAsync(destino, cancelacion);

        return destino.ToArray();
    }

    /// <summary>
    /// Primer filtro: forma de la dirección y esquema.
    /// </summary>
    /// <remarks>
    /// Limitar a <c>http</c> y <c>https</c> corta de raíz <c>file://</c>, que leería
    /// archivos del servidor, y esquemas como <c>gopher://</c> que se han usado para
    /// hablar con servicios internos. La comprobación de a qué IP se conecta es otra
    /// cosa y va en el adaptador, porque solo se puede hacer al conectar.
    /// </remarks>
    private static bool TryLeerDireccion(string? indicada, out Uri direccion)
    {
        direccion = null!;

        if (string.IsNullOrWhiteSpace(indicada))
        {
            return false;
        }

        if (!Uri.TryCreate(indicada.Trim(), UriKind.Absolute, out var leida))
        {
            return false;
        }

        if (leida.Scheme != Uri.UriSchemeHttp && leida.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        direccion = leida;
        return true;
    }
}
