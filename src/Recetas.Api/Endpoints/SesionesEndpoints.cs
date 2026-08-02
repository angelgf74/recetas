using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Recetas.Aplicacion.Sesiones;
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

        // Endpoint de prueba de que el JWT funciona. La 003 traerá los de verdad.
        rutas.MapGet("/yo", Identidad)
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

    private static IResult Identidad(ClaimsPrincipal usuario)
    {
        var id = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);

        var correo = usuario.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? usuario.FindFirstValue(ClaimTypes.Email);

        if (!Guid.TryParse(id, out var identificador) || correo is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new RespuestaDeIdentidad(identificador, correo));
    }
}
