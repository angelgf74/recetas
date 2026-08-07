using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Recetas.Aplicacion.Cuentas;
using Recetas.Aplicacion.Sesiones;
using Recetas.Contratos.Cuentas;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Endpoints;

public static class SesionesEndpoints
{
    /// <summary>
    /// Un único mensaje para "ese correo no existe" y "la contraseña es incorrecta".
    /// Distinguirlos revelaría qué direcciones tienen cuenta.
    /// </summary>
    private const string MensajeDeCredencialesIncorrectas = "Correo o contraseña incorrectos.";

    public static void MapearEndpointsDeSesiones(this IEndpointRouteBuilder rutas)
    {
        rutas.MapPost("/sesiones", IniciarAsync)
            .RequireRateLimiting(LimitesDePeticiones.InicioDeSesion)
            .WithTags("Sesiones");

        rutas.MapGet("/yo", IdentidadAsync)
            .RequireAuthorization()
            .WithTags("Sesiones");

        // Borrarse es borrar este recurso, no invocar un verbo. Y no lleva
        // identificador en la ruta porque nadie borra a otro: el sujeto sale del
        // token, y un `/usuarios/{id}` solo abriría la duda de qué pasa si el id
        // no es el tuyo.
        rutas.MapDelete("/yo", BorrarmeAsync)
            .RequireAuthorization()
            .WithTags("Sesiones");
    }

    private static async Task<IResult> IniciarAsync(
        [FromBody] PeticionDeInicioDeSesion peticion,
        IniciarSesion casoDeUso,
        CancellationToken cancelacion)
    {
        var acceso = await casoDeUso.EjecutarAsync(peticion.Correo, peticion.Contrasena, cancelacion);

        if (acceso is null)
        {
            return Results.Json(
                new RespuestaDeError(MensajeDeCredencialesIncorrectas),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new RespuestaDeInicioDeSesion(acceso.Token, acceso.Caducidad));
    }

    private static async Task<IResult> IdentidadAsync(
        ClaimsPrincipal usuario,
        ConsultarResumenDeLaCuenta resumenDeLaCuenta,
        CancellationToken cancelacion)
    {
        var id = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);

        var correo = usuario.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? usuario.FindFirstValue(ClaimTypes.Email);

        if (!Guid.TryParse(id, out var identificador) || correo is null)
        {
            return Results.Unauthorized();
        }

        // El recuento va aquí y no en un endpoint aparte porque la pantalla que lo
        // necesita —la de baja— ya pide esto para saber quién es.
        var resumen = await resumenDeLaCuenta.EjecutarAsync(identificador, cancelacion);

        return Results.Ok(new RespuestaDeIdentidad(
            identificador,
            correo,
            resumen.Recetas,
            resumen.Fotos));
    }

    private static async Task<IResult> BorrarmeAsync(
        [FromBody] PeticionDeBaja peticion,
        ClaimsPrincipal usuario,
        BorrarCuenta casoDeUso,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var resultado = await casoDeUso.EjecutarAsync(usuarioId, peticion.Contrasena, cancelacion);

        return resultado switch
        {
            ResultadoDeBaja.Correcto => Results.NoContent(),

            // Mismo 401 para "la contraseña no es" y "esa cuenta ya no está": quien
            // pregunta tiene un token válido, así que la única lectura útil es que
            // vuelva a identificarse.
            _ => Results.Json(
                new RespuestaDeError("La contraseña no es correcta."),
                statusCode: StatusCodes.Status401Unauthorized)
        };
    }
}
