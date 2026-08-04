using System.Net;
using System.Net.Sockets;

namespace Recetas.Infraestructura.Importacion;

/// <summary>
/// Decide si una IP está en internet o dentro de la red del servidor.
/// </summary>
/// <remarks>
/// Es la pieza que impide que la importación (feature 011) se convierta en una
/// falsificación de petición desde el servidor. Sin esto, cualquier usuario con
/// sesión podría hacer que la API pidiera <c>http://127.0.0.1:5432</c> o
/// <c>http://169.254.169.254/</c>, es decir, hablar con PostgreSQL o con el
/// servicio de metadatos de la máquina desde fuera del cortafuegos.
/// <para>
/// Tipo propio y público para poder probarlo solo: la lista de rangos es la clase
/// de cosa en la que se cuela un olvido, y un olvido aquí es un agujero.
/// </para>
/// </remarks>
public static class ComprobadorDeDireccionesPublicas
{
    public static bool EsPublica(IPAddress direccion)
    {
        ArgumentNullException.ThrowIfNull(direccion);

        // Una IPv4 escrita como IPv6 (::ffff:127.0.0.1) es el disfraz clásico para
        // saltarse una lista que solo mire IPv4.
        if (direccion.IsIPv4MappedToIPv6)
        {
            direccion = direccion.MapToIPv4();
        }

        return direccion.AddressFamily switch
        {
            AddressFamily.InterNetwork => EsPublicaV4(direccion),
            AddressFamily.InterNetworkV6 => EsPublicaV6(direccion),

            // Cualquier otra familia no tiene sentido aquí.
            _ => false
        };
    }

    private static bool EsPublicaV4(IPAddress direccion)
    {
        var o = direccion.GetAddressBytes();

        // Los rangos se comprueban con la máscara exacta que les corresponde. Es
        // más verboso que mirar solo los dos primeros octetos, pero bloquear de más
        // también tiene coste: 203.0.0.0/16 es espacio asignado y de verdad, y solo
        // 203.0.113.0/24 es documentación.
        return !(
            // 0.0.0.0/8 — "esta red". Muchos sistemas la tratan como loopback.
            o[0] == 0

            // 10.0.0.0/8 — privada.
            || o[0] == 10

            // 127.0.0.0/8 — bucle local. El objetivo más obvio.
            || o[0] == 127

            // 169.254.0.0/16 — enlace local. Incluye 169.254.169.254, el servicio
            // de metadatos de AWS, Azure y GCP.
            || (o[0] == 169 && o[1] == 254)

            // 172.16.0.0/12 — privada.
            || (o[0] == 172 && o[1] is >= 16 and <= 31)

            // 192.168.0.0/16 — privada.
            || (o[0] == 192 && o[1] == 168)

            // 192.0.0.0/24 — asignaciones de protocolo IETF.
            || (o[0] == 192 && o[1] == 0 && o[2] == 0)

            // 192.0.2.0/24 — documentación (TEST-NET-1).
            || (o[0] == 192 && o[1] == 0 && o[2] == 2)

            // 198.18.0.0/15 — pruebas de rendimiento entre redes.
            || (o[0] == 198 && o[1] is 18 or 19)

            // 198.51.100.0/24 — documentación (TEST-NET-2).
            || (o[0] == 198 && o[1] == 51 && o[2] == 100)

            // 203.0.113.0/24 — documentación (TEST-NET-3).
            || (o[0] == 203 && o[1] == 0 && o[2] == 113)

            // 100.64.0.0/10 — CGNAT, la red del operador.
            || (o[0] == 100 && o[1] is >= 64 and <= 127)

            // 224.0.0.0/4 multidifusión y 240.0.0.0/4 reservada, que incluye
            // 255.255.255.255.
            || o[0] >= 224);
    }

    private static bool EsPublicaV6(IPAddress direccion)
    {
        if (IPAddress.IPv6Loopback.Equals(direccion)
            || IPAddress.IPv6Any.Equals(direccion)
            || direccion.IsIPv6LinkLocal
            || direccion.IsIPv6SiteLocal
            || direccion.IsIPv6Multicast
            || direccion.IsIPv6Teredo)
        {
            return false;
        }

        var octetos = direccion.GetAddressBytes();

        // fc00::/7 — direcciones locales únicas, el equivalente a las privadas de
        // IPv4. No hay propiedad en .NET que las cubra.
        if ((octetos[0] & 0xFE) == 0xFC)
        {
            return false;
        }

        // 64:ff9b::/96 — NAT64: envuelve una IPv4 que hay que mirar por dentro.
        if (octetos[0] == 0x00 && octetos[1] == 0x64 && octetos[2] == 0xFF && octetos[3] == 0x9B)
        {
            return EsPublica(new IPAddress(octetos[12..16]));
        }

        return true;
    }
}
