using System.Security.Claims;
using Microsoft.Extensions.Options;
using Recetas.Aplicacion.Recetas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Sesiones;
using Recetas.Dominio.Recetas;
using Recetas.Infraestructura.Fotos;

namespace Recetas.Api.Endpoints;

public static class FotosEndpoints
{
    private const string MensajeDeNoEncontrada = "No se ha encontrado esa foto.";

    public static void MapearEndpointsDeFotos(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/recetas/{recetaId:guid}/fotos")
            .RequireAuthorization()
            .WithTags("Fotos");

        grupo.MapPost("/", SubirAsync)
            // El cuerpo se lee como flujo: no interesa que ASP.NET intente enlazar
            // un formulario ni cargar el archivo entero por su cuenta.
            .DisableAntiforgery();

        grupo.MapGet("/{fotoId:guid}", DescargarAsync);
        grupo.MapGet("/{fotoId:guid}/miniatura", DescargarMiniaturaAsync);
        grupo.MapDelete("/{fotoId:guid}", BorrarAsync);
    }

    private static async Task<IResult> SubirAsync(
        Guid recetaId,
        HttpRequest peticion,
        ClaimsPrincipal usuario,
        GestionDeFotos gestion,
        IOptions<OpcionesDeFotos> opciones,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        // El contenido se toma del cuerpo tal cual, sin mirar el nombre de archivo
        // ni el Content-Type que envíe el cliente: el formato se deduce después de
        // los bytes de cabecera.
        var (resultado, foto) = await gestion.SubirAsync(
            usuarioId,
            recetaId,
            peticion.Body,
            opciones.Value.TamanoMaximoEnBytes,
            cancelacion);

        return resultado switch
        {
            ResultadoDeFoto.Correcto when foto is not null =>
                Results.Created(
                    $"/recetas/{recetaId}/fotos/{foto.Id}",
                    new FotoRespuesta(foto.Id, foto.Tipo.ToString(), foto.TamanoEnBytes)),

            ResultadoDeFoto.NoEncontrada => NoEncontrada(),

            ResultadoDeFoto.DemasiadoGrande => Results.Json(
                new RespuestaDeError(
                    $"La foto supera el máximo de {opciones.Value.TamanoMaximoEnBytes / (1024 * 1024)} MB."),
                statusCode: StatusCodes.Status413PayloadTooLarge),

            _ => Results.BadRequest(new RespuestaDeError(
                "El archivo no es una imagen JPEG, PNG o WebP."))
        };
    }

    private static async Task<IResult> DescargarAsync(
        Guid recetaId,
        Guid fotoId,
        ClaimsPrincipal usuario,
        GestionDeFotos gestion,
        HttpResponse respuesta,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var (resultado, foto) = await gestion.ObtenerAsync(usuarioId, recetaId, fotoId, cancelacion);

        return Servir(resultado, foto, respuesta);
    }

    /// <summary>
    /// Versión reducida, para los listados. Mismo grupo y mismas reglas: una
    /// miniatura es tan privada como la foto de la que sale.
    /// </summary>
    private static async Task<IResult> DescargarMiniaturaAsync(
        Guid recetaId,
        Guid fotoId,
        ClaimsPrincipal usuario,
        GestionDeFotos gestion,
        HttpResponse respuesta,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var (resultado, foto) = await gestion.ObtenerMiniaturaAsync(usuarioId, recetaId, fotoId, cancelacion);

        return Servir(resultado, foto, respuesta);
    }

    private static IResult Servir(ResultadoDeFoto resultado, FotoDescargada? foto, HttpResponse respuesta)
    {
        if (resultado is not ResultadoDeFoto.Correcto || foto is null)
        {
            return NoEncontrada();
        }

        // El tipo de contenido lo pone el servidor a partir de lo que detectó al
        // subir, nunca el cliente. Con nosniff, además, el navegador no puede
        // reinterpretar el archivo como otra cosa.
        respuesta.Headers.XContentTypeOptions = "nosniff";

        // Ninguna respuesta con contenido de un usuario debe quedar cacheada en el
        // borde de Cloudflare: una foto privada servida a otro sería una fuga.
        respuesta.Headers.CacheControl = "private, no-store";

        return Results.Stream(foto.Contenido, foto.Tipo.TipoDeContenido());
    }

    private static async Task<IResult> BorrarAsync(
        Guid recetaId,
        Guid fotoId,
        ClaimsPrincipal usuario,
        GestionDeFotos gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var resultado = await gestion.BorrarAsync(usuarioId, recetaId, fotoId, cancelacion);

        return resultado is ResultadoDeFoto.Correcto ? Results.NoContent() : NoEncontrada();
    }

    private static IResult NoEncontrada() =>
        Results.Json(new RespuestaDeError(MensajeDeNoEncontrada), statusCode: StatusCodes.Status404NotFound);
}
