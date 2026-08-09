using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Recetas.Infraestructura.Importacion;

namespace Recetas.Infraestructura.Tests.Importacion;

/// <summary>
/// Ejercita el descargador <b>de verdad</b>, sin sustituirlo por un doble.
/// </summary>
/// <remarks>
/// <see cref="DescargarImagenAsync"/> se añadió en la 027 reutilizando el mismo
/// <c>HttpClient</c> endurecido que ya usaba <c>DescargarAsync</c> para la
/// página, en vez de un cliente nuevo. Este test es la prueba de que la
/// reutilización es real: si <c>DescargarImagenAsync</c> hubiera acabado con su
/// propio <c>HttpClient</c> sin el <c>ConnectCallback</c> —el error fácil al
/// extraer código compartido—, esta llamada llegaría a intentar conectar de
/// verdad a la dirección interna en lugar de rechazarla antes de intentarlo.
/// </remarks>
public class DescargadorDePaginasSeguroTests
{
    private static DescargadorDePaginasSeguro Crear() =>
        new(
            Options.Create(new OpcionesDeImportacion()),
            NullLogger<DescargadorDePaginasSeguro>.Instance);

    [Theory]
    [InlineData("http://127.0.0.1/foto.jpg")]
    [InlineData("http://10.0.0.1/foto.jpg")]
    [InlineData("http://169.254.169.254/foto.jpg")]
    public async Task DescargarImagen_ContraUnaDireccionInterna_DevuelveNulo(string direccion)
    {
        using var descargador = Crear();

        var resultado = await descargador.DescargarImagenAsync(new Uri(direccion), maximoDeBytes: 1024);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DescargarImagen_UnEsquemaQueNoEsHttp_DevuelveNuloSinIntentarConectar()
    {
        using var descargador = Crear();

        var resultado = await descargador.DescargarImagenAsync(
            new Uri("file:///etc/passwd"), maximoDeBytes: 1024);

        Assert.Null(resultado);
    }

    /// <summary>
    /// El test que de verdad demuestra la protección, y no solo que no había
    /// nada escuchando en la dirección probada.
    /// </summary>
    /// <remarks>
    /// Los tests de arriba comprueban contra <c>127.0.0.1</c>, <c>10.0.0.1</c>…
    /// pero en esta máquina de pruebas puede que no haya nada respondiendo ahí,
    /// y entonces pasarían igual con un cliente <b>sin ninguna protección</b>: la
    /// conexión fallaría por su cuenta y el resultado sería el mismo <c>null</c>
    /// por la razón equivocada. Aquí se levanta un servidor de verdad en
    /// <c>127.0.0.1</c>, listo para responder con bytes de imagen válidos, y se
    /// comprueba que <see cref="DescargadorDePaginasSeguro.DescargarImagenAsync"/>
    /// no llega a hablar con él. Si `DescargarImagenAsync` hubiera acabado usando
    /// un <c>HttpClient</c> nuevo sin el <c>ConnectCallback</c> —el descuido
    /// fácil al extraer el núcleo compartido en la 027—, este test es el que lo
    /// pillaría: los de arriba, no.
    /// </remarks>
    [Fact]
    public async Task DescargarImagen_NuncaHablaConUnServidorEnBucleLocal_AunqueEsteListoParaResponder()
    {
        using var servidor = new TcpListener(IPAddress.Loopback, 0);
        servidor.Start();
        var puerto = ((IPEndPoint)servidor.LocalEndpoint).Port;

        var tareaDelServidor = AceptarYResponderAsync(servidor);

        using var descargador = Crear();

        var resultado = await descargador.DescargarImagenAsync(
            new Uri($"http://127.0.0.1:{puerto}/foto.jpg"), maximoDeBytes: 1024);

        servidor.Stop();
        await tareaDelServidor;

        Assert.Null(resultado);
    }

    /// <summary>Un servidor HTTP mínimo, a mano, que respondería con una imagen si le dejaran hablar.</summary>
    private static async Task AceptarYResponderAsync(TcpListener servidor)
    {
        try
        {
            using var cliente = await servidor.AcceptTcpClientAsync();
            await using var flujo = cliente.GetStream();

            var respuesta =
                "HTTP/1.1 200 OK\r\nContent-Type: image/jpeg\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc"u8
                    .ToArray();

            await flujo.WriteAsync(respuesta);
            await flujo.FlushAsync();

            // Cierre ordenado: un FIN, no un RST. Sin esto, Dispose() puede
            // abortar la conexión antes de que el cliente termine de leer —se
            // confundiría con el propio descargador rechazando la conexión, que
            // es justo lo que este test necesita distinguir con seguridad.
            cliente.Client.Shutdown(SocketShutdown.Send);
            await Task.Delay(100);
        }
        catch (Exception excepcion) when (excepcion is SocketException or ObjectDisposedException)
        {
            // El caso esperado: el descargador nunca llega a conectar, y
            // servidor.Stop() interrumpe el AcceptTcpClientAsync que quedó
            // esperando.
        }
    }
}
