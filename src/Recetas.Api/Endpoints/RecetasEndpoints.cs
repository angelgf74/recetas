using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Recetas.Aplicacion.Favoritos;
using Recetas.Aplicacion.Moderacion;
using Recetas.Aplicacion.Recetas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Sesiones;
using Recetas.Dominio.Recetas;
using Recetas.Infraestructura.Fotos;
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
        // Aquí y no en /yo porque devuelve recetas: va con las otras consultas de
        // conjunto. El restrictor de "/{id:guid}" impide que choquen.
        grupo.MapGet("/favoritas", ListarFavoritasAsync);
        // Un recurso propio y no un verbo colgando de /recetas: esto NO crea una
        // receta, devuelve un borrador que el usuario aún tiene que revisar.
        grupo.MapPost("/importaciones", ImportarAsync)
            .RequireRateLimiting(LimitesDePeticiones.Importacion);

        grupo.MapGet("/{id:guid}", ObtenerAsync);
        grupo.MapPut("/{id:guid}", ActualizarAsync);
        grupo.MapDelete("/{id:guid}", BorrarAsync);

        // Publicar es un endpoint propio y no un campo del PUT: si la visibilidad
        // viajara en el cuerpo de la edición, cualquier cliente que reenviara la
        // receta entera podría publicarla sin querer. Compartir tiene que ser un
        // acto deliberado, que es lo que pide mission.md.
        grupo.MapPost("/{id:guid}/publicacion", (Guid id, ClaimsPrincipal usuario, GestionDeRecetas gestion,
            CancellationToken cancelacion) => CambiarVisibilidadAsync(id, usuario, gestion, true, cancelacion));

        // El DELETE lo usan dos actores con la misma ruta: el autor, que deja de
        // compartir su receta, y el responsable del servicio, que retira una
        // ajena. Para el usuario es la misma acción —dejar de estar público— y no
        // merece un endpoint de administración aparte, pero por dentro son casos
        // de uso distintos: la retirada avisa por correo al autor (020).
        grupo.MapDelete("/{id:guid}/publicacion", DespublicarAsync);

        // PUT y DELETE, al revés que la publicación, que es POST: publicar es un
        // acto que ocurre una vez y marcar es un estado que se fija. Que repetir la
        // llamada responda igual no es una concesión, es lo que significa PUT.
        grupo.MapPut("/{id:guid}/favorito", MarcarFavoritaAsync);
        grupo.MapDelete("/{id:guid}/favorito", DesmarcarFavoritaAsync);
    }

    private static async Task<IResult> ListarFavoritasAsync(
        ClaimsPrincipal usuario,
        GestionDeFavoritos favoritos,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var recetas = await favoritos.ListarMisFavoritasAsync(usuarioId, cancelacion);

        return Results.Ok(recetas.Select(receta => AResumen(receta, usuarioId)));
    }

    private static async Task<IResult> MarcarFavoritaAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeFavoritos favoritos,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var resultado = await favoritos.MarcarAsync(usuarioId, id, cancelacion);

        return resultado is ResultadoDeFavorito.Correcto ? Results.NoContent() : NoEncontrada();
    }

    /// <summary>
    /// Quita la marca. Responde <c>204</c> aunque no estuviera marcada, e incluso
    /// aunque la receta ya no exista: el estado final es el que se pedía.
    /// </summary>
    private static async Task<IResult> DesmarcarFavoritaAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeFavoritos favoritos,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        await favoritos.DesmarcarAsync(usuarioId, id, cancelacion);

        return Results.NoContent();
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

    private static async Task<IResult> DespublicarAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        RetirarPorModeracion retirar,
        CorreoDelResponsable responsable,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        // Se intenta primero como autor, que es el caso normal. Solo si no es
        // suya y quien pide es el responsable, se pasa por la moderación: así
        // el responsable despublicando una receta propia sigue siendo el autor
        // despublicando, y no se autoenvía un aviso de retirada.
        var resultado = await gestion.CambiarVisibilidadAsync(usuarioId, id, publicar: false, cancelacion);

        if (resultado is ResultadoDeReceta.Correcto)
        {
            return Results.NoContent();
        }

        if (!responsable.Es(usuario.ObtenerCorreo()))
        {
            return NoEncontrada();
        }

        var retirada = await retirar.EjecutarAsync(usuarioId, id, cancelacion);

        return retirada is ResultadoDeRetirada.Correcto ? Results.NoContent() : NoEncontrada();
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

        // `ingrediente` y `etiqueta` se repiten en la consulta:
        // ?ingrediente=tomate&ingrediente=albahaca&etiqueta=rapido
        var ingredientes = peticion.Query["ingrediente"].Where(valor => valor is not null).ToList()!;
        var etiquetas = peticion.Query["etiqueta"].Where(valor => valor is not null).ToList()!;

        var criterios = CriteriosDeBusqueda.Crear(nombre, ingredientes!, tipoDePlato, etiquetas!);

        var (resultados, hayMas) = await gestion.BuscarAsync(usuarioId, criterios, cancelacion);

        return Results.Ok(new RespuestaDeBusqueda(
            resultados.Select(receta => AResumen(receta, usuarioId)).ToList(),
            hayMas));
    }

    /// <param name="raciones">
    /// Ajusta las cantidades a ese número de comensales. Va como parámetro de
    /// consulta y no como endpoint aparte porque escalar es <b>leer</b> la receta de
    /// otra manera: no se guarda nada.
    /// </param>
    private static async Task<IResult> ObtenerAsync(
        Guid id,
        ClaimsPrincipal usuario,
        GestionDeRecetas gestion,
        GestionDeFavoritos favoritos,
        CorreoDelResponsable responsable,
        CancellationToken cancelacion,
        int? raciones = null)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        // Se valida antes de tocar nada. Un factor disparatado no rompería la base
        // de datos —no se escribe— pero devolver cantidades absurdas sin decir nada
        // es peor que un error claro.
        if (raciones is { } pedidas
            && pedidas is < PeticionDeReceta.RacionesMinimas or > PeticionDeReceta.RacionesMaximas)
        {
            return Results.BadRequest(new RespuestaDeError(
                $"Las raciones deben estar entre {PeticionDeReceta.RacionesMinimas} " +
                $"y {PeticionDeReceta.RacionesMaximas}."));
        }

        var (resultado, receta) = await gestion.ObtenerAsync(usuarioId, id, cancelacion);

        if (resultado is not ResultadoDeReceta.Correcto || receta is null)
        {
            return NoEncontrada();
        }

        // Después de comprobar que puede verla, nunca antes: preguntando por el
        // favorito de una receta que no se puede ver no se aprende nada, pero es
        // una consulta que no hay motivo para hacer.
        var esFavorita = await favoritos.EsFavoritaAsync(usuarioId, id, cancelacion);

        return Results.Ok(ARespuesta(
            receta,
            usuarioId,
            raciones,
            responsable.Es(usuario.ObtenerCorreo()),
            esFavorita));
    }

    /// <summary>
    /// Lee una receta de una página web y devuelve un borrador.
    /// </summary>
    /// <remarks>
    /// No guarda nada. El usuario revisa el resultado en el formulario y es su
    /// envío el que crea la receta, que además nace privada como cualquier otra.
    /// </remarks>
    private static async Task<IResult> ImportarAsync(
        [FromBody] PeticionDeImportacion peticion,
        ClaimsPrincipal usuario,
        ImportarReceta casoDeUso,
        IOptions<OpcionesDeFotos> opcionesDeFotos,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out _))
        {
            return Results.Unauthorized();
        }

        // Mismo tope que rige las fotos que sube el propio usuario (027): no hay
        // una configuración aparte para las importadas.
        var (resultado, receta, origen) = await casoDeUso.EjecutarAsync(
            peticion.Direccion, opcionesDeFotos.Value.TamanoMaximoEnBytes, cancelacion);

        return resultado switch
        {
            ResultadoDeImportacion.Correcto when receta is not null && origen is not null =>
                Results.Ok(new RespuestaDeImportacion(
                    receta.Nombre,
                    receta.Elaboracion,
                    receta.Raciones,
                    receta.Ingredientes
                        .Select(linea => new LineaDeIngredientePeticion(
                            linea.Nombre, linea.Cantidad, linea.Unidad.ToString()))
                        .ToList(),
                    origen.ToString(),
                    receta.ImagenLimpia)),

            ResultadoDeImportacion.DireccionNoValida =>
                Results.BadRequest(new RespuestaDeError(
                    "Esa dirección no vale. Pega el enlace completo de la receta, empezando por https://")),

            ResultadoDeImportacion.SinReceta =>
                Results.Json(
                    new RespuestaDeError(
                        "Esa página se ha abierto, pero no publica la receta en un formato que sepamos leer. "
                        + "Tendrás que copiarla a mano."),
                    statusCode: StatusCodes.Status422UnprocessableEntity),

            // Un único mensaje para "no responde", "no existe", "no es HTML" y
            // "apunta a la red interna". Distinguirlos convertiría este endpoint en
            // un escáner de la red del servidor manejado desde fuera.
            _ => Results.Json(
                new RespuestaDeError("No se ha podido abrir esa página. Comprueba la dirección."),
                statusCode: StatusCodes.Status502BadGateway)
        };
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

        if (peticion.Raciones is { } raciones
            && raciones is < PeticionDeReceta.RacionesMinimas or > PeticionDeReceta.RacionesMaximas)
        {
            return false;
        }

        datos = new DatosDeReceta(
            peticion.Nombre, tipo, peticion.Elaboracion, lineas, peticion.Raciones, peticion.Etiquetas);
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

    /// <param name="racionesPedidas">
    /// Raciones a las que ajustar las cantidades, o <c>null</c> para devolverlas tal
    /// como están guardadas. La receta no se modifica en ningún caso.
    /// </param>
    /// <param name="esResponsable">
    /// Si quien pregunta es el responsable del servicio. Solo sirve para decirle al
    /// cliente si ofrecer la retirada; el permiso se vuelve a comprobar al pedirla.
    /// </param>
    /// <param name="esFavorita">Si quien pregunta la tiene marcada. Dato suyo, no de la receta.</param>
    private static RespuestaDeReceta ARespuesta(
        DominioReceta receta,
        Guid usuarioId,
        int? racionesPedidas = null,
        bool esResponsable = false,
        bool esFavorita = false) =>
        new(
            receta.Id,
            receta.Nombre,
            receta.TipoDePlato.ToString(),
            receta.Elaboracion,
            receta.Visibilidad.ToString(),
            receta.FechaDeCreacion,
            receta.FechaDeModificacion,
            receta.EscalarA(racionesPedidas)
                .Select(linea => new LineaDeIngredienteRespuesta(
                    linea.Ingrediente?.Nombre.Valor ?? string.Empty,
                    linea.Cantidad,
                    linea.Unidad.ToString()))
                .OrderBy(linea => linea.Nombre, StringComparer.Ordinal)
                .ToList(),
            receta.Fotos
                .OrderBy(foto => foto.FechaDeSubida)
                .Select(foto => new FotoRespuesta(
                    foto.Id, foto.Tipo.ToString(), foto.TamanoEnBytes, foto.Id == receta.FotoDePortada?.Id))
                .ToList(),
            receta.EsDe(usuarioId),
            receta.Raciones,
            receta.RacionesMostradasPara(racionesPedidas),

            // Solo sobre lo ajeno y ya publicado: sobre lo propio ya están las
            // acciones de siempre, y sobre lo privado no hay nada que retirar.
            esResponsable && !receta.EsDe(usuarioId) && receta.EsPublica,
            esFavorita,
            receta.Etiquetas
                .Select(vinculo => vinculo.Etiqueta?.Nombre.Valor ?? string.Empty)
                .Where(nombre => nombre.Length > 0)
                .OrderBy(nombre => nombre, StringComparer.Ordinal)
                .ToList());

    private static IResult NoEncontrada() =>
        Results.Json(new RespuestaDeError(MensajeDeNoEncontrada), statusCode: StatusCodes.Status404NotFound);

    private static IResult DatosNoValidos() =>
        Results.BadRequest(new RespuestaDeError(
            "Revisa los datos de la receta: nombre, tipo de plato, elaboración e ingredientes."));
}
