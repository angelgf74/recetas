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
    /// Verificar la contraseña actual es la misma superficie de ataque que el
    /// inicio de sesión, así que comparte forma. Cubo propio: agotar el de
    /// inicio de sesión no debe bloquear a alguien que solo quiere cambiar su
    /// contraseña, y viceversa.
    /// </summary>
    public const string CambioDeContrasena = "cambio-de-contrasena";

    /// <summary>
    /// Importar desde una URL hace que el servidor pida una página elegida por el
    /// usuario. Sin límite, la API es un intermediario anónimo para machacar a un
    /// tercero, y el tráfico sale con la IP del servidor.
    /// </summary>
    public const string Importacion = "importacion";

    /// <summary>
    /// Cada denuncia manda un correo al responsable del servicio. Sin límite, el
    /// endpoint es una forma cómoda de inundarle el buzón, y el remitente sería el
    /// propio dominio del proyecto.
    /// </summary>
    public const string Denuncia = "denuncia";

    /// <summary>
    /// Exportar los datos lee del disco todas las fotos del usuario y las
    /// comprime. Es la petición más cara del producto, y una que en la vida de
    /// una cuenta se hace una vez o ninguna.
    /// </summary>
    public const string Exportacion = "exportacion";

    public static void AnadirLimitesDePeticiones(this IServiceCollection servicios, IConfiguration configuracion)
    {
        // Configurables para que los tests puedan elevarlos y no chocar con el
        // límite al encadenar peticiones, y bajarlos donde se prueba el 429.
        var maximoDeRegistros = configuracion.GetValue("Limites:RegistrosPorVentana", 5);
        var maximoDeInicios = configuracion.GetValue("Limites:IniciosDeSesionPorVentana", 10);
        var maximoDeCambiosDeContrasena = configuracion.GetValue("Limites:CambiosDeContrasenaPorVentana", 10);
        var maximoDeImportaciones = configuracion.GetValue("Limites:ImportacionesPorVentana", 20);
        var maximoDeDenuncias = configuracion.GetValue("Limites:DenunciasPorVentana", 10);
        var maximoDeExportaciones = configuracion.GetValue("Limites:ExportacionesPorVentana", 3);

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

            opciones.AddPolicy(CambioDeContrasena, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = maximoDeCambiosDeContrasena,
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

            opciones.AddPolicy(Denuncia, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        // Denunciar es raro: quien encuentra algo que denunciar lo
                        // hace una vez, no diez veces por hora. La ventana es larga
                        // porque cada denuncia cuesta un correo.
                        PermitLimit = maximoDeDenuncias,
                        Window = TimeSpan.FromMinutes(60)
                    }));

            opciones.AddPolicy(Exportacion, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveDeParticion(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        // Tres por hora: suficiente para reintentar si la descarga
                        // se corta, y lejos de permitir que alguien use el
                        // endpoint para hacer trabajar al disco sin parar.
                        PermitLimit = maximoDeExportaciones,
                        Window = TimeSpan.FromMinutes(60)
                    }));
        });
    }

    /// <summary>
    /// Se reparte por dirección de origen.
    /// </summary>
    /// <remarks>
    /// Tras el túnel de Cloudflare, <c>RemoteIpAddress</c> sería la del propio
    /// túnel si no se configuraran cabeceras de reenvío, y entonces <b>todos los
    /// usuarios compartirían cubo</b>: bastaría uno activo para dejar sin alta ni
    /// inicio de sesión a los demás. Por eso nginx sobrescribe
    /// <c>X-Forwarded-For</c> con la cabecera de Cloudflare y <c>Program</c>
    /// configura <c>ForwardedHeaders</c> con un único salto de confianza.
    /// <para>
    /// <b>Comprobado en producción el 9 de agosto de 2026:</b> agotado el límite
    /// desde una IP externa, una petición directa a Kestrel desde el propio
    /// servidor seguía respondiendo <c>401</c> y no <c>429</c>. Sin las cabeceras
    /// ambas habrían caído en el cubo de <c>127.0.0.1</c>.
    /// </para>
    /// </remarks>
    private static string ClaveDeParticion(HttpContext contexto) =>
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
}
