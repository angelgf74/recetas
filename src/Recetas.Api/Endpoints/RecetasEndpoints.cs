using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Recetas.Aplicacion.Recetas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Sesiones;
using Recetas.Dominio.Recetas;
using DominioReceta = Recetas.Dominio.Recetas.Receta;

namespace Recetas.Api.Endpoints;

public static class RecetasEndpoints
{
    /// <summary>
    /// Mismo texto para "no existe" y "no es tuya". Distinguirlos permitiría
    /// averiguar qué identificadores corresponden a recetas reales de otros.
    /// </summary>
    private const string MensajeDeNoEncontrada = "No se ha encontrado esa receta.";

    public static void MapearEndpointsDeRecetas(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/recetas")
            .RequireAuthorization()
            .WithTags("Recetas");

        grupo.MapPost("/", CrearAsync);
        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:guid}", ObtenerAsync);
        grupo.MapPut("/{id:guid}", ActualizarAsync);
        grupo.MapDelete("/{id:guid}", BorrarAsync);

        // Publicar es un endpoint propio y no un campo del PUT: si la visibilidad
        // viajara en el cuerpo de la edición, cualquier cliente que reenviara la
        // receta entera podría publicarla sin querer. Compartir tiene que ser un
        // acto deliberado, que es lo que pide mission.md.
        grupo.MapPost("/{id:guid}/publicacion", (Guid id, ClaimsPrincipal usuario, GestionDeRecetas gestion,
            CancellationToken cancelacion) => CambiarVisibilidadAsync(id, usuario, gestion, true, cancelacion));

        grupo.MapDelete("/{id:guid}/publicacion", (Guid id, ClaimsPrincipal usuario, GestionDeRecetas gestion,
            CancellationToken cancelacion) => CambiarVisibilidadAsync(id, usuario, gestion, false, cancelacion));
    }

    private static async Task<IResult> CambiarVisibilidadAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        bool publicar,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var resultado = await gestion.CambiarVisibilidadAsync(usuarioId, id, publicar, cancelacion);

        return resultado is ResultadoDeReceta.Correcto ? Results.NoContent() : NoEncontrada();
    }

    private static async Task<IResult> CrearAsync(
        [FromBody] PeticionDeReceta peticion,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var autorId))
        {
            return Results.Unauthorized();
        }

        if (!TryTraducir(peticion, out var datos))
        {
            return DatosNoValidos();
        }

        var (resultado, receta) = await gestion.CrearAsync(autorId, datos, cancelacion);

        return resultado is ResultadoDeReceta.Correcto && receta is not null
            ? Results.Created($"/recetas/{receta.Id}", ARespuesta(receta))
            : DatosNoValidos();
    }

    private static async Task<IResult> ListarAsync(
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var recetas = await gestion.ListarMiasAsync(usuarioId, cancelacion);

        return Results.Ok(recetas.Select(receta => new ResumenDeReceta(
            receta.Id,
            receta.Nombre,
            receta.TipoDePlato.ToString(),
            receta.Visibilidad.ToString(),
            receta.FechaDeModificacion)));
    }

    private static async Task<IResult> ObtenerAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var (resultado, receta) = await gestion.ObtenerAsync(usuarioId, id, cancelacion);

        return resultado is ResultadoDeReceta.Correcto && receta is not null
            ? Results.Ok(ARespuesta(receta))
            : NoEncontrada();
    }

    private static async Task<IResult> ActualizarAsync(
        Guid id,
        [FromBody] PeticionDeReceta peticion,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        if (!TryTraducir(peticion, out var datos))
        {
            return DatosNoValidos();
        }

        var resultado = await gestion.ActualizarAsync(usuarioId, id, datos, cancelacion);

        return resultado switch
        {
            ResultadoDeReceta.Correcto => Results.NoContent(),
            ResultadoDeReceta.NoEncontrada => NoEncontrada(),
            _ => DatosNoValidos()
        };
    }

    private static async Task<IResult> BorrarAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var resultado = await gestion.BorrarAsync(usuarioId, id, cancelacion);

        return resultado is ResultadoDeReceta.Correcto ? Results.NoContent() : NoEncontrada();
    }

    /// <summary>
    /// Traduce el contrato público al del dominio. Los enumerados llegan como
    /// texto, así que aquí es donde se rechaza un valor fuera de la lista cerrada.
    /// </summary>
    private static bool TryTraducir(PeticionDeReceta peticion, out DatosDeReceta datos)
    {
        datos = null!;

        if (!Enum.TryParse<TipoDePlato>(peticion.TipoDePlato, ignoreCase: true, out var tipo)
            || !Enum.IsDefined(tipo))
        {
            return false;
        }

        var lineas = new List<LineaDeIngrediente>(peticion.Ingredientes.Count);

        foreach (var linea in peticion.Ingredientes)
        {
            if (!Enum.TryParse<Unidad>(linea.Unidad, ignoreCase: true, out var unidad)
                || !Enum.IsDefined(unidad))
            {
                return false;
            }

            lineas.Add(new LineaDeIngrediente(linea.Nombre, linea.Cantidad, unidad));
        }

        datos = new DatosDeReceta(peticion.Nombre, tipo, peticion.Elaboracion, lineas);
        return true;
    }

    private static RespuestaDeReceta ARespuesta(DominioReceta receta) =>
        new(
            receta.Id,
            receta.Nombre,
            receta.TipoDePlato.ToString(),
            receta.Elaboracion,
            receta.Visibilidad.ToString(),
            receta.FechaDeCreacion,
            receta.FechaDeModificacion,
            receta.Ingredientes
                .Select(linea => new LineaDeIngredienteRespuesta(
                    linea.Ingrediente?.Nombre.Valor ?? string.Empty,
                    linea.Cantidad,
                    linea.Unidad.ToString()))
                .OrderBy(linea => linea.Nombre, StringComparer.Ordinal)
                .ToList(),
            receta.Fotos
                .OrderBy(foto => foto.FechaDeSubida)
                .Select(foto => new FotoRespuesta(foto.Id, foto.Tipo.ToString(), foto.TamanoEnBytes))
                .ToList());

    private static IResult NoEncontrada() =>
        Results.Json(new RespuestaDeError(MensajeDeNoEncontrada), statusCode: StatusCodes.Status404NotFound);

    private static IResult DatosNoValidos() =>
        Results.BadRequest(new RespuestaDeError(
            "Revisa los datos de la receta: nombre, tipo de plato, elaboración e ingredientes."));
}
