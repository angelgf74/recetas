using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Web.Servicios;

/// <summary>
/// Envoltorio de las llamadas a la API. Traduce respuestas HTTP a resultados
/// que las páginas puedan mostrar, sin que cada una repita el manejo de errores.
/// </summary>
public sealed class ClienteDeApi(HttpClient http)
{
    public Task<ResultadoDeLlamada> SolicitarAltaAsync(string correo) =>
        EnviarAsync("registro/solicitudes", new PeticionDeSolicitudDeRegistro { Correo = correo });

    public Task<ResultadoDeLlamada> CompletarAltaAsync(string token, string contrasena) =>
        EnviarAsync("registro/completar",
            new PeticionDeCompletarRegistro { Token = token, Contrasena = contrasena });

    public async Task<(ResultadoDeLlamada Resultado, RespuestaDeInicioDeSesion? Acceso)> IniciarSesionAsync(
        string correo,
        string contrasena)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync("sesiones",
                new PeticionDeInicioDeSesion { Correo = correo, Contrasena = contrasena });

            if (respuesta.IsSuccessStatusCode)
            {
                var acceso = await respuesta.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
                return (ResultadoDeLlamada.Correcto(), acceso);
            }

            return (await TraducirErrorAsync(respuesta), null);
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), null);
        }
    }

    private async Task<ResultadoDeLlamada> EnviarAsync<TPeticion>(string ruta, TPeticion peticion)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync(ruta, peticion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return await TraducirErrorAsync(respuesta);
            }

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeError>();
            return ResultadoDeLlamada.Correcto(cuerpo?.Mensaje);
        }
        catch (HttpRequestException)
        {
            return ResultadoDeLlamada.Fallido(MensajeDeRedCaida);
        }
    }

    private static async Task<ResultadoDeLlamada> TraducirErrorAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return ResultadoDeLlamada.Fallido("Demasiados intentos. Espera unos minutos y vuelve a probar.");
        }

        try
        {
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeError>();

            if (!string.IsNullOrWhiteSpace(cuerpo?.Mensaje))
            {
                return ResultadoDeLlamada.Fallido(cuerpo.Mensaje);
            }
        }
        catch (Exception) when (respuesta.StatusCode is HttpStatusCode.BadRequest)
        {
            // La validación automática de ASP.NET responde en otro formato.
            return ResultadoDeLlamada.Fallido("Revisa los datos introducidos.");
        }

        return ResultadoDeLlamada.Fallido("No se ha podido completar la operación. Inténtalo de nuevo.");
    }

    private const string MensajeDeRedCaida =
        "No se ha podido contactar con el servidor. Comprueba tu conexión.";
}

/// <param name="Correcta">Si la operación salió bien.</param>
/// <param name="Mensaje">Texto a mostrar al usuario, ya sea de éxito o de error.</param>
public sealed record ResultadoDeLlamada(bool Correcta, string? Mensaje)
{
    public static ResultadoDeLlamada Correcto(string? mensaje = null) => new(true, mensaje);

    public static ResultadoDeLlamada Fallido(string mensaje) => new(false, mensaje);
}
