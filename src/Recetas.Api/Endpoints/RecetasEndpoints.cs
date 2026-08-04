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

        // Antes que "/{id:guid}" no hace falta por el restrictor de ruta, pero se
        // declara aquí junto al listado porque son las dos consultas de conjunto.
        grupo.MapGet("/busqueda", BuscarAsync);
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
            ? Results.Created($"/recetas/{receta.Id}", ARespuesta(receta, autorId))
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

        return Results.Ok(recetas.Select(receta => AResumen(receta, usuarioId)));
    }

    private static async Task<IResult> BuscarAsync(
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        HttpRequest peticion,
        CancellationToken cancelacion,
        string? nombre = null,
        string? tipo = null)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        TipoDePlato? tipoDePlato = null;

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<TipoDePlato>(tipo, ignoreCase: true, out var valor) || !Enum.IsDefined(valor))
            {
                return Results.BadRequest(new RespuestaDeError("Ese tipo de plato no existe."));
            }

            tipoDePlato = valor;
        }

        // `ingrediente` se repite en la consulta: ?ingrediente=tomate&ingrediente=albahaca
        var ingredientes = peticion.Query["ingrediente"].Where(valor => valor is not null).ToList()!;

        var criterios = CriteriosDeBusqueda.Crear(nombre, ingredientes!, tipoDePlato);

        var (resultados, hayMas) = await gestion.BuscarAsync(usuarioId, criterios, cancelacion);

        return Results.Ok(new RespuestaDeBusqueda(
            resultados.Select(receta => AResumen(receta, usuarioId)).ToList(),
            hayMas));
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
            ? Results.Ok(ARespuesta(receta, usuarioId))
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

    private static ResumenDeReceta AResumen(DominioReceta receta, Guid usuarioId) =>
        new(
            receta.Id,
            receta.Nombre,
            receta.TipoDePlato.ToString(),
            receta.Visibilidad.ToString(),
            receta.FechaDeModificacion,
            receta.EsDe(usuarioId),
            receta.FotoDePortada?.Id);

    private static RespuestaDeReceta ARespuesta(DominioReceta receta, Guid usuarioId) =>
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
                .ToList(),
            receta.EsDe(usuarioId));

    private static IResult NoEncontrada() =>
        Results.Json(new RespuestaDeError(MensajeDeNoEncontrada), statusCode: StatusCodes.Status404NotFound);

    private static IResult DatosNoValidos() =>
        Results.BadRequest(new RespuestaDeError(
            "Revisa los datos de la receta: nombre, tipo de plato, elaboración e ingredientes."));
}
