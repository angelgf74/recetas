using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Recetas.Aplicacion.Moderacion;
using Recetas.Contratos.Moderacion;
using Recetas.Contratos.Sesiones;
using Recetas.Dominio.Moderacion;

namespace Recetas.Api.Endpoints;

/// <summary>
/// Denunciar una receta pública ajena.
/// </summary>
/// <remarks>
/// Recurso propio colgando de la receta, como las fotos, y no un verbo suelto: la
/// denuncia es un hecho con autor, motivo y fecha, no una acción sin rastro.
/// </remarks>
public static class DenunciasEndpoints
{
    public static void MapearEndpointsDeDenuncias(this IEndpointRouteBuilder rutas)
    {
        rutas.MapPost("/recetas/{recetaId:guid}/denuncias", DenunciarAsync)
            .RequireAuthorization()
            .RequireRateLimiting(LimitesDePeticiones.Denuncia)
            .WithTags("Denuncias");
    }

    private static async Task<IResult> DenunciarAsync(
        Guid recetaId,
        [FromBody] PeticionDeDenuncia peticion,
        ClaimsPrincipal usuario,
        GestionDeDenuncias gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var denuncianteId))
        {
            return Results.Unauthorized();
        }

        // El motivo llega como texto. Si no es uno de los conocidos se rechaza en
        // vez de caer en un valor por defecto: una denuncia archivada bajo un
        // motivo que el usuario no eligió es peor que un error claro.
        if (!Enum.TryParse<MotivoDeDenuncia>(peticion.Motivo, ignoreCase: true, out var motivo)
            || !Enum.IsDefined(motivo))
        {
            return Results.BadRequest(new RespuestaDeError("Ese motivo de denuncia no existe."));
        }

        var resultado = await gestion.DenunciarAsync(
            denuncianteId,
            recetaId,
            motivo,
            peticion.Comentario,
            cancelacion);

        return resultado switch
        {
            ResultadoDeDenuncia.Correcto => Results.NoContent(),

            ResultadoDeDenuncia.EsPropia => Results.BadRequest(new RespuestaDeError(
                "Esta receta es tuya: si no quieres que se vea, despublícala.")),

            // Mismo texto que en el resto de la API para "no existe" y "no puedes
            // verla". Distinguirlos convertiría esto en una forma de averiguar qué
            // recetas privadas existen.
            _ => Results.Json(
                new RespuestaDeError("No se ha encontrado esa receta."),
                statusCode: StatusCodes.Status404NotFound)
        };
    }
}
