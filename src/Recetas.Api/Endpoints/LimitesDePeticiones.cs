using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Recetas.Api.Endpoints;

/// <summary>
/// Límites de frecuencia. Sin ellos, el paso 1 del alta es un cañón de correo
/// contra direcciones de terceros, y el inicio de sesión un banco de pruebas
/// para adivinar contraseñas.
/// </summary>
public static class LimitesDePeticiones
{
    public const string Registro = "registro";
    public const string InicioDeSesion = "inicio-de-sesion";

    /// <summary>
    /// Importar desde una URL hace que el servidor pida una página elegida por el
    /// usuario. Sin límite, la API es un intermediario anónimo para machacar a un
    /// tercero, y el tráfico sale con la IP del servidor.
    /// </summary>
    public const string Importacion = "importacion";

    public static void AnadirLimitesDePeticiones(this IServiceCollection servicios, IConfiguration configuracion)
    {
        // Configurables para que los tests puedan elevarlos y no chocar con el
        // límite al encadenar peticiones, y bajarlos donde se prueba el 429.
        var maximoDeRegistros = configuracion.GetValue("Limites:RegistrosPorVentana", 5);
        var maximoDeInicios = configuracion.GetValue("Limites:IniciosDeSesionPorVentana", 10);
        var maximoDeImportaciones = configuracion.GetValue("Limites:ImportacionesPorVentana", 20);

        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opciones.AddPolicy(Registro, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = maximoDeRegistros,
                        Window = TimeSpan.FromMinutes(15)
                    }));

            opciones.AddPolicy(InicioDeSesion, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = maximoDeInicios,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            opciones.AddPolicy(Importacion, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        // Holgado para un uso normal —se importa una receta de vez
                        // en cuando— y estrecho para usar la API como ariete.
                        PermitLimit = maximoDeImportaciones,
                        Window = TimeSpan.FromMinutes(5)
                    }));
        });
    }

    /// <summary>
    /// Se reparte por dirección de origen.
    /// </summary>
    /// <remarks>
    /// Limitación conocida: tras el túnel de Cloudflare, <c>RemoteIpAddress</c> es
    /// la del túnel salvo que se configuren cabeceras de reenvío, con lo que todos
    /// los usuarios compartirían cubo. Al desplegar hay que configurar
    /// <c>ForwardedHeaders</c>; hasta entonces el límite es conservador pero global.
    /// </remarks>
    private static string ClaveDeParticion(HttpContext contexto) =>
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
}
