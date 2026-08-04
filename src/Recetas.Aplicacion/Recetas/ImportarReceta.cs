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
public sealed class ImportarReceta(IDescargadorDePaginas descargador)
{
    public async Task<(ResultadoDeImportacion Resultado, RecetaImportada? Receta, Uri? Origen)> EjecutarAsync(
        string? direccionIndicada,
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

        return receta is null
            ? (ResultadoDeImportacion.SinReceta, null, direccion)
            : (ResultadoDeImportacion.Correcto, receta, direccion);
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
