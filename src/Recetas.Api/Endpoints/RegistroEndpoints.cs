using Microsoft.AspNetCore.Mvc;
using Recetas.Aplicacion.Registro;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;
using Recetas.Infraestructura.Correo;
using Microsoft.Extensions.Options;

namespace Recetas.Api.Endpoints;

public static class RegistroEndpoints
{
    /// <summary>
    /// Mensaje del paso 1. Deliberadamente ambiguo: es el mismo tanto si se ha
    /// creado una solicitud como si el correo ya tenía cuenta.
    /// </summary>
    private const string MensajeDeSolicitudAceptada =
        "Si esa dirección es válida, te hemos enviado un correo con las instrucciones.";

    /// <summary>Mismo texto para token inexistente, manipulado, caducado o ya usado.</summary>
    private const string MensajeDeEnlaceNoValido =
        "Este enlace no es válido o ha caducado. Pide el alta de nuevo.";

    public static void MapearEndpointsDeRegistro(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/registro").WithTags("Registro");

        grupo.MapPost("/solicitudes", SolicitarAsync)
            .RequireRateLimiting(LimitesDePeticiones.Registro);

        grupo.MapPost("/completar", CompletarAsync);
    }

    private static async Task<IResult> SolicitarAsync(
        [FromBody] PeticionDeSolicitudDeRegistro peticion,
        SolicitarRegistro casoDeUso,
        IOptions<OpcionesDeCorreo> opcionesDeCorreo,
        CancellationToken cancelacion)
    {
        var baseDeLaWeb = opcionesDeCorreo.Value.BaseDeLaWeb.TrimEnd('/');

        var resultado = await casoDeUso.EjecutarAsync(
            peticion.Correo,
            token => $"{baseDeLaWeb}/registro/completar?token={Uri.EscapeDataString(token)}",
            cancelacion);

        return resultado switch
        {
            ResultadoDeSolicitud.CorreoInvalido =>
                Results.BadRequest(new RespuestaDeError("Ese correo electrónico no parece válido.")),

            // Aceptada tanto si se ha creado la solicitud como si el correo ya
            // tenía cuenta: distinguirlos convertiría esto en un comprobador de
            // qué direcciones están registradas.
            _ => Results.Ok(new RespuestaDeError(MensajeDeSolicitudAceptada))
        };
    }

    private static async Task<IResult> CompletarAsync(
        [FromBody] PeticionDeCompletarRegistro peticion,
        CompletarRegistro casoDeUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoDeUso.EjecutarAsync(peticion.Token, peticion.Contrasena, cancelacion);

        return resultado switch
        {
            ResultadoDeCompletarRegistro.Completado =>
                Results.Ok(new RespuestaDeError("Cuenta creada. Ya puedes iniciar sesión.")),

            ResultadoDeCompletarRegistro.ContrasenaNoValida =>
                Results.BadRequest(new RespuestaDeError(
                    $"La contraseña debe tener entre {PeticionDeCompletarRegistro.LongitudMinimaDeContrasena} " +
                    $"y {PeticionDeCompletarRegistro.LongitudMaximaDeContrasena} caracteres.")),

            _ => Results.BadRequest(new RespuestaDeError(MensajeDeEnlaceNoValido))
        };
    }
}
