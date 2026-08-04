using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Recetas.Aplicacion.Contrasenas;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;
using Recetas.Infraestructura.Correo;

namespace Recetas.Api.Endpoints;

public static class ContrasenasEndpoints
{
    /// <summary>
    /// Mensaje del paso 1. Deliberadamente ambiguo: es el mismo tanto si se ha
    /// enviado un enlace como si esa dirección no tiene cuenta.
    /// </summary>
    private const string MensajeDeSolicitudAceptada =
        "Si esa dirección tiene cuenta, te hemos enviado un correo con las instrucciones.";

    /// <summary>Mismo texto para token inexistente, manipulado, caducado o ya usado.</summary>
    private const string MensajeDeEnlaceNoValido =
        "Este enlace no es válido o ha caducado. Pide otro desde el inicio de sesión.";

    public static void MapearEndpointsDeContrasenas(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/contrasena").WithTags("Contrasena");

        // Comparte cubo con el alta a propósito: los dos endpoints hacen lo mismo
        // —mandar correo a una dirección que no es la de quien llama—, así que el
        // límite debe contar el total. Con cubos separados, un mismo origen podría
        // duplicar los envíos alternando entre ambos.
        grupo.MapPost("/solicitudes", SolicitarAsync)
            .RequireRateLimiting(LimitesDePeticiones.Registro);

        grupo.MapPost("/restablecer", RestablecerAsync);
    }

    private static async Task<IResult> SolicitarAsync(
        [FromBody] PeticionDeSolicitudDeContrasena peticion,
        SolicitarRestablecerContrasena casoDeUso,
        IOptions<OpcionesDeCorreo> opcionesDeCorreo,
        CancellationToken cancelacion)
    {
        var baseDeLaWeb = opcionesDeCorreo.Value.BaseDeLaWeb.TrimEnd('/');

        var resultado = await casoDeUso.EjecutarAsync(
            peticion.Correo,
            token => $"{baseDeLaWeb}/contrasena/nueva?token={Uri.EscapeDataString(token)}",
            cancelacion);

        return resultado switch
        {
            ResultadoDeSolicitudDeContrasena.CorreoInvalido =>
                Results.BadRequest(new RespuestaDeError("Ese correo electrónico no parece válido.")),

            // Aceptada tenga cuenta o no: distinguirlos convertiría esto en un
            // comprobador de qué direcciones están registradas.
            _ => Results.Ok(new RespuestaDeError(MensajeDeSolicitudAceptada))
        };
    }

    private static async Task<IResult> RestablecerAsync(
        [FromBody] PeticionDeRestablecerContrasena peticion,
        RestablecerContrasena casoDeUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoDeUso.EjecutarAsync(peticion.Token, peticion.Contrasena, cancelacion);

        return resultado switch
        {
            ResultadoDeRestablecerContrasena.Restablecida =>
                Results.Ok(new RespuestaDeError("Contraseña cambiada. Ya puedes iniciar sesión con ella.")),

            ResultadoDeRestablecerContrasena.ContrasenaNoValida =>
                Results.BadRequest(new RespuestaDeError(
                    $"La contraseña debe tener entre {PeticionDeCompletarRegistro.LongitudMinimaDeContrasena} " +
                    $"y {PeticionDeCompletarRegistro.LongitudMaximaDeContrasena} caracteres.")),

            _ => Results.BadRequest(new RespuestaDeError(MensajeDeEnlaceNoValido))
        };
    }
}
