using System.Net;
using Recetas.Infraestructura.Importacion;

namespace Recetas.Infraestructura.Tests.Importacion;

/// <summary>
/// Es el test que sostiene la feature 011: sin este filtro, la importación deja a
/// cualquier usuario con sesión pedir cosas desde dentro de la máquina del
/// servidor, saltándose el cortafuegos.
/// </summary>
public class ComprobadorDeDireccionesPublicasTests
{
    [Theory]
    // Bucle local: el objetivo obvio. 127.0.0.1:54009 es el propio Kestrel.
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("0.0.0.0")]

    // Privadas: la red del servidor. PostgreSQL vive aquí.
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.1.1")]

    // Enlace local, con el servicio de metadatos de nube dentro.
    [InlineData("169.254.1.1")]
    [InlineData("169.254.169.254")]

    // CGNAT: la red del operador, tampoco es internet.
    [InlineData("100.64.0.1")]

    // Documentación y pruebas.
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]

    // Multidifusión y reservadas.
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("255.255.255.255")]
    public void LasDireccionesInternas_SeRechazan(string direccion)
    {
        Assert.False(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    public void LasDireccionesInternasIPv6_SeRechazan(string direccion)
    {
        Assert.False(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }

    /// <summary>
    /// El disfraz clásico: escribir una IPv4 interna como IPv6 para colarse por una
    /// lista que solo mire IPv4.
    /// </summary>
    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.0.0.1")]
    public void UnaIPv4InternaDisfrazadaDeIPv6_SeRechaza(string direccion)
    {
        Assert.False(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }

    /// <summary>NAT64 envuelve una IPv4 dentro de una IPv6: hay que mirar dentro.</summary>
    [Fact]
    public void UnaIPv4InternaDentroDeNat64_SeRechaza()
    {
        Assert.False(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse("64:ff9b::127.0.0.1")));
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("172.32.0.1")]      // Justo fuera de 172.16.0.0/12.
    [InlineData("192.169.0.1")]     // Justo fuera de 192.168.0.0/16.
    [InlineData("100.128.0.1")]     // Justo fuera de 100.64.0.0/10.
    [InlineData("223.255.255.255")] // Justo antes de la multidifusión.
    public void LasDireccionesDeInternet_SePermiten(string direccion)
    {
        Assert.True(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }

    /// <summary>
    /// Los rangos de documentación son /24, no /16: bloquear de más dejaría fuera
    /// espacio asignado y real.
    /// </summary>
    [Theory]
    [InlineData("203.0.114.1")]
    [InlineData("198.51.101.1")]
    [InlineData("192.0.3.1")]
    public void LosVecinosDeLosRangosDeDocumentacion_SePermiten(string direccion)
    {
        Assert.True(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }

    [Theory]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("2001:4860:4860::8888")]
    public void LasDireccionesIPv6DeInternet_SePermiten(string direccion)
    {
        Assert.True(ComprobadorDeDireccionesPublicas.EsPublica(IPAddress.Parse(direccion)));
    }
}
