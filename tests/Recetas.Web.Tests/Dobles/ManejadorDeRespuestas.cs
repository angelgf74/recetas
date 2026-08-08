using System.Net;
using System.Net.Http.Json;

namespace Recetas.Web.Tests.Dobles;

/// <summary>
/// Manejador HTTP que responde con lo que se le haya preparado, por ruta.
/// </summary>
/// <remarks>
/// Es la costura por la que se prueba la web sin red. Se dobla el
/// <see cref="HttpClient"/> y no el <c>ClienteDeApi</c> a propósito: extraer una
/// interfaz de este último añadiría una abstracción a producción solo para
/// comodidad del test, y de paso dejaría sin probar la traducción de respuestas
/// HTTP que ese cliente hace.
/// </remarks>
public sealed class ManejadorDeRespuestas : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _respuestas = new();

    /// <summary>Rutas que se han pedido, en orden. Útil para afirmar qué llamó un componente.</summary>
    public List<string> Peticiones { get; } = [];

    /// <summary>Prepara una respuesta con cuerpo JSON para una ruta.</summary>
    public ManejadorDeRespuestas Para<T>(string ruta, T cuerpo, HttpStatusCode codigo = HttpStatusCode.OK)
    {
        _respuestas[Normalizar(ruta)] = () => new HttpResponseMessage(codigo)
        {
            Content = JsonContent.Create(cuerpo)
        };

        return this;
    }

    /// <summary>Prepara una respuesta sin cuerpo.</summary>
    public ManejadorDeRespuestas Para(string ruta, HttpStatusCode codigo)
    {
        _respuestas[Normalizar(ruta)] = () => new HttpResponseMessage(codigo);

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage peticion,
        CancellationToken cancelacion)
    {
        var ruta = Normalizar(peticion.RequestUri!.PathAndQuery);

        Peticiones.Add(ruta);

        if (_respuestas.TryGetValue(ruta, out var respuesta))
        {
            return Task.FromResult(respuesta());
        }

        // Un 404 silencioso escondería que al test le falta preparar una ruta, y
        // el componente se limitaría a pintar "no encontrado" sin decir por qué.
        throw new InvalidOperationException(
            $"El test no preparó ninguna respuesta para '{ruta}'. " +
            $"Preparadas: {string.Join(", ", _respuestas.Keys)}");
    }

    private static string Normalizar(string ruta) => ruta.TrimStart('/');
}
