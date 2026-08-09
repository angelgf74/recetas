using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Importacion;

/// <summary>
/// Trae el HTML de una página, sin poder alcanzar la red interna del servidor.
/// </summary>
/// <remarks>
/// Hasta la feature 011 el servidor solo hablaba con PostgreSQL y con Brevo, dos
/// destinos fijos. Este adaptador le hace pedir una dirección <b>que escribe un
/// usuario</b>, y eso es falsificación de petición desde el servidor (SSRF) en su
/// forma más pura: conseguir que la API pida cosas desde dentro de la máquina,
/// donde no llega el cortafuegos.
/// <para>
/// Todas las defensas de aquí son necesarias y cada una tapa un hueco distinto; no
/// se quita ninguna sin entender cuál.
/// </para>
/// </remarks>
public sealed class DescargadorDePaginasSeguro : IDescargadorDePaginas, IDisposable
{
    private readonly HttpClient _cliente;
    private readonly OpcionesDeImportacion _opciones;
    private readonly ILogger<DescargadorDePaginasSeguro> _registro;

    public DescargadorDePaginasSeguro(
        IOptions<OpcionesDeImportacion> opciones,
        ILogger<DescargadorDePaginasSeguro> registro)
    {
        _opciones = opciones.Value;
        _registro = registro;

        var manejador = new SocketsHttpHandler
        {
            // A mano, para poder comprobar CADA destino. En automático, el primer
            // salto puede ser público y el segundo apuntar a 127.0.0.1.
            AllowAutoRedirect = false,

            // Sin cookies: no hay sesión que mantener con el sitio de origen, y
            // guardarlas sería llevarse estado de una petición a la siguiente.
            UseCookies = false,

            ConnectTimeout = TimeSpan.FromSeconds(5),

            // El punto clave. Se comprueba la IP EN EL MOMENTO DE CONECTAR, no al
            // resolver el nombre. Validar tras resolver deja la puerta abierta al
            // "DNS rebinding": entre la comprobación y la conexión, el nombre puede
            // resolver a otra cosa. Aquí se conecta exactamente a la IP validada.
            ConnectCallback = ConectarSoloAPublicasAsync
        };

        _cliente = new HttpClient(manejador)
        {
            Timeout = TimeSpan.FromSeconds(_opciones.SegundosDeEspera)
        };

        // Identificarse es de buena educación y además permite al sitio de origen
        // bloquearnos si no quiere que se importen sus recetas.
        _cliente.DefaultRequestHeaders.UserAgent.ParseAdd("RecetasBot/1.0 (+https://recetas.angelgf.com.es)");
        _cliente.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
    }

    public async Task<string?> DescargarAsync(Uri direccion, CancellationToken cancelacion = default)
    {
        var resultado = await DescargarBytesAsync(
            direccion,
            respuesta => respuesta.Content.Headers.ContentType?.MediaType is "text/html" or "application/xhtml+xml",
            _opciones.MaximoDeBytes,
            cancelacion);

        return resultado is null
            ? null
            : Decodificar(resultado.Value.Bytes, resultado.Value.Codificacion);
    }

    public async Task<byte[]?> DescargarImagenAsync(
        Uri direccion, long maximoDeBytes, CancellationToken cancelacion = default)
    {
        // Sin filtro de Content-Type aquí a propósito: ver el comentario de
        // IDescargadorDePaginas.DescargarImagenAsync. El tope de bytes no sale de
        // aquí: lo trae quien llama, para que sea el mismo que rige las fotos que
        // sube el propio usuario.
        var resultado = await DescargarBytesAsync(direccion, respuesta => true, maximoDeBytes, cancelacion);

        return resultado?.Bytes;
    }

    /// <summary>
    /// Núcleo compartido: sigue redirecciones a mano, valida cada destino al
    /// conectar (vía <see cref="ConectarSoloAPublicasAsync"/>, colgado del mismo
    /// <see cref="_cliente"/> para las dos operaciones) y corta la lectura por
    /// bytes. <see cref="DescargarAsync"/> y <see cref="DescargarImagenAsync"/> no
    /// son dos implementaciones parecidas: son la misma, con un filtro de
    /// contenido y un destino distintos.
    /// </summary>
    private async Task<(byte[] Bytes, string? Codificacion)?> DescargarBytesAsync(
        Uri direccion,
        Func<HttpResponseMessage, bool> tipoAceptado,
        long maximoDeBytes,
        CancellationToken cancelacion)
    {
        var actual = direccion;

        try
        {
            for (var salto = 0; salto <= _opciones.MaximoDeRedirecciones; salto++)
            {
                if (actual.Scheme != Uri.UriSchemeHttp && actual.Scheme != Uri.UriSchemeHttps)
                {
                    return null;
                }

                using var peticion = new HttpRequestMessage(HttpMethod.Get, actual);
                using var respuesta = await _cliente.SendAsync(
                    peticion, HttpCompletionOption.ResponseHeadersRead, cancelacion);

                if (EsRedireccion(respuesta.StatusCode))
                {
                    var destino = respuesta.Headers.Location;

                    if (destino is null)
                    {
                        return null;
                    }

                    // Relativa respecto a la actual, como manda HTTP.
                    actual = destino.IsAbsoluteUri ? destino : new Uri(actual, destino);
                    continue;
                }

                if (!respuesta.IsSuccessStatusCode)
                {
                    return null;
                }

                if (!tipoAceptado(respuesta))
                {
                    return null;
                }

                var bytes = await LeerBytesAsync(respuesta, maximoDeBytes, cancelacion);

                return bytes is null
                    ? null
                    : (bytes, respuesta.Content.Headers.ContentType?.CharSet);
            }

            // Se han agotado los saltos: cadena de redirecciones demasiado larga,
            // o un bucle.
            return null;
        }
        catch (Exception excepcion) when (
            excepcion is HttpRequestException or TaskCanceledException or InvalidOperationException
                or UriFormatException or SocketException)
        {
            // Se registra el motivo real para poder diagnosticar, pero al usuario
            // le llega un único mensaje: ver el comentario de IDescargadorDePaginas.
            _registro.LogInformation(
                excepcion, "No se pudo descargar {Direccion} para importar.", direccion);

            return null;
        }
    }

    private static async Task<byte[]?> LeerBytesAsync(
        HttpResponseMessage respuesta, long maximoDeBytes, CancellationToken cancelacion)
    {
        // El Content-Length lo escribe el servidor de destino y puede mentir, así
        // que se comprueba antes de leer y además se corta la lectura por bytes.
        if (respuesta.Content.Headers.ContentLength > maximoDeBytes)
        {
            return null;
        }

        await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);

        var memoria = new MemoryStream();
        var bufer = new byte[8192];
        int leidos;

        while ((leidos = await flujo.ReadAsync(bufer, cancelacion)) > 0)
        {
            if (memoria.Length + leidos > maximoDeBytes)
            {
                // Se corta y se descarta: pasar del tope no es un documento válido
                // de importar, y seguir leyendo es regalar memoria.
                return null;
            }

            memoria.Write(bufer, 0, leidos);
        }

        return memoria.ToArray();
    }

    /// <summary>
    /// Interpreta los bytes con la codificación que declare la respuesta, y con
    /// UTF-8 si no dice nada o dice algo que no se conoce.
    /// </summary>
    private static string Decodificar(byte[] bytes, string? codificacion)
    {
        if (!string.IsNullOrWhiteSpace(codificacion))
        {
            try
            {
                return Encoding.GetEncoding(codificacion.Trim('"')).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Nombre de codificación inventado o no registrado: se sigue con UTF-8.
            }
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static bool EsRedireccion(HttpStatusCode estado) =>
        estado is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    /// <summary>
    /// Resuelve el nombre, descarta las IP que no son públicas y conecta a una de
    /// las que quedan.
    /// </summary>
    /// <remarks>
    /// Va aquí y no antes de la petición a propósito: es el único punto en el que
    /// se puede garantizar que la IP comprobada es la misma a la que se conecta.
    /// </remarks>
    private static async ValueTask<Stream> ConectarSoloAPublicasAsync(
        SocketsHttpConnectionContext contexto,
        CancellationToken cancelacion)
    {
        var destino = contexto.DnsEndPoint;

        var direcciones = IPAddress.TryParse(destino.Host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(destino.Host, cancelacion);

        var publicas = direcciones.Where(ComprobadorDeDireccionesPublicas.EsPublica).ToArray();

        if (publicas.Length == 0)
        {
            // El mensaje no sale de aquí: quien llama lo convierte en el error
            // genérico. Si el usuario pudiera distinguir este caso, el endpoint
            // sería un escáner de la red del servidor.
            throw new HttpRequestException("La dirección no apunta a un destino público.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(publicas, destino.Port, cancelacion);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public void Dispose() => _cliente.Dispose();
}
