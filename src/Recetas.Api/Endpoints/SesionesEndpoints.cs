using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Recetas.Aplicacion.Cuentas;
using Recetas.Contratos.Cuentas;
using Recetas.Dominio.Recetas;
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

        rutas.MapGet("/yo/datos", ExportarAsync)
            .RequireAuthorization()
            .RequireRateLimiting(LimitesDePeticiones.Exportacion)
            .WithTags("Sesiones");
    }

    /// <summary>
    /// Entrega en un <c>.zip</c> todo lo que el producto guarda del usuario.
    /// </summary>
    /// <remarks>
    /// El paquete se escribe <b>directamente sobre el cuerpo de la respuesta</b>,
    /// sin construirlo antes en memoria ni en un archivo temporal: una cuenta con
    /// treinta fotos de 8 MB son 240 MB, y cargarlos para comprimirlos tumbaría el
    /// servidor.
    /// <para>
    /// La contrapartida está aceptada y anotada en el plan de la 019: si algo
    /// falla a mitad, la descarga queda truncada y ya no se puede responder un
    /// error, porque las cabeceras se enviaron al principio. Un ZIP incompleto se
    /// nota al abrirlo —el índice va al final—, que es mejor que un archivo que
    /// parece bueno y no lo es.
    /// </para>
    /// </remarks>
    private static async Task<IResult> ExportarAsync(
        ClaimsPrincipal usuario,
        ExportarMisDatos casoDeUso,
        HttpResponse respuesta,
        CancellationToken cancelacion)
    {
        if (!usuario.TryObtenerId(out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var datos = await casoDeUso.EjecutarAsync(usuarioId, cancelacion);

        if (datos is null)
        {
            return Results.Unauthorized();
        }

        var nombreDelArchivo = $"recetas-{DateTimeOffset.UtcNow:yyyy-MM-dd}.zip";

        respuesta.ContentType = "application/zip";
        respuesta.Headers.ContentDisposition = $"attachment; filename=\"{nombreDelArchivo}\"";

        using var paquete = new ZipArchive(respuesta.BodyWriter.AsStream(), ZipArchiveMode.Create);

        await EscribirTextoAsync(paquete, "LEEME.txt", TextoDeAyuda(datos), cancelacion);

        var recetas = new List<RecetaExportada>();

        foreach (var receta in datos.Recetas)
        {
            var nombresDeFotos = new List<string>();

            foreach (var foto in receta.Fotos.OrderBy(foto => foto.FechaDeSubida))
            {
                var nombre = $"fotos/{foto.Id:N}.{foto.Tipo.Extension()}";

                // Que falte un archivo no aborta la exportación: es mejor
                // entregar el resto que negar todos sus datos por una foto.
                await using var contenido = await casoDeUso.AbrirFotoAsync(foto, cancelacion);

                if (contenido is null)
                {
                    continue;
                }

                // Nivel rápido: las fotos ya son JPEG o PNG, así que volver a
                // comprimirlas gasta procesador para no ganar tamaño.
                var entrada = paquete.CreateEntry(nombre, CompressionLevel.Fastest);

                await using var destino = entrada.Open();
                await contenido.CopyToAsync(destino, cancelacion);

                nombresDeFotos.Add(nombre);
            }

            recetas.Add(new RecetaExportada(
                receta.Nombre,
                receta.TipoDePlato.ToString(),
                receta.Raciones,
                receta.Visibilidad.ToString(),
                [.. receta.Ingredientes
                    .Select(linea => new IngredienteExportado(
                        linea.Ingrediente?.Nombre.Valor ?? string.Empty,
                        linea.Cantidad,
                        linea.Unidad.ToString()))
                    .OrderBy(linea => linea.Nombre, StringComparer.Ordinal)],
                receta.Elaboracion,
                receta.FechaDeCreacion,
                receta.FechaDeModificacion,
                nombresDeFotos));
        }

        var contenidoJson = JsonSerializer.Serialize(
            new DatosExportados(datos.Correo, datos.FechaDeAlta, DateTimeOffset.UtcNow, recetas),
            OpcionesDeExportacion);

        await EscribirTextoAsync(paquete, "datos.json", contenidoJson, cancelacion);

        // Ya se ha escrito en el cuerpo: devolver cualquier otro resultado ahora
        // sería intentar responder dos veces.
        return Results.Empty;
    }

    /// <summary>
    /// Legible por una persona: con sangría y sin escapar los acentos, que es lo
    /// que hace `JsonSerializer` por defecto y convierte "Bizcocho de plátano" en
    /// algo ilegible.
    /// </summary>
    private static readonly JsonSerializerOptions OpcionesDeExportacion = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static async Task EscribirTextoAsync(
        ZipArchive paquete,
        string nombre,
        string contenido,
        CancellationToken cancelacion)
    {
        var entrada = paquete.CreateEntry(nombre, CompressionLevel.Optimal);

        await using var destino = entrada.Open();
        await using var escritor = new StreamWriter(destino, Encoding.UTF8);

        await escritor.WriteAsync(contenido.AsMemory(), cancelacion);
    }

    private static string TextoDeAyuda(DatosDelUsuario datos) =>
        $"""
         TUS DATOS DE RECETAS
         ====================

         Esto es todo lo que Recetas guarda de la cuenta {datos.Correo}.

         Qué hay aquí dentro:

         - datos.json  Tus recetas: nombre, tipo de plato, raciones, ingredientes
                       con sus cantidades, la elaboración tal como la escribiste, y
                       si estaba compartida o era solo tuya.
         - fotos/      Tus fotos, tal como las subiste. Cada receta indica en
                       datos.json cuáles son las suyas.

         Qué NO hay:

         - Tu contraseña. No se guarda tal cual y no serviría de nada.
         - Recetas de otras personas, aunque las hubieras podido ver.

         El archivo datos.json se abre con cualquier editor de texto. Si lo que
         quieres es leer tus recetas, ábrelo con el Bloc de notas y busca por el
         nombre de la que te interese.

         OJO: este paquete lleva tus fotos y tu correo. Guárdalo donde guardarías
         cualquier cosa personal, y piénsatelo antes de enviarlo por ahí.

         Generado el {DateTimeOffset.Now:dd/MM/yyyy} · https://recetas.angelgf.com.es
         """;

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
