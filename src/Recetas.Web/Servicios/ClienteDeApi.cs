using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Cuentas;
using Recetas.Contratos.Moderacion;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Web.Servicios;

/// <summary>
/// Envoltorio de las llamadas a la API. Traduce respuestas HTTP a resultados que
/// las páginas puedan mostrar, sin que cada una repita el manejo de errores.
/// </summary>
public sealed class ClienteDeApi(HttpClient http)
{
    private const string MensajeDeRedCaida =
        "No se ha podido contactar con el servidor. Comprueba tu conexión.";

    // ------------------------------------------------------------- Cuentas

    public Task<ResultadoDeLlamada> SolicitarAltaAsync(string correo) =>
        EnviarAsync(HttpMethod.Post, "registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

    public Task<ResultadoDeLlamada> CompletarAltaAsync(string token, string contrasena) =>
        EnviarAsync(HttpMethod.Post, "registro/completar",
            new PeticionDeCompletarRegistro { Token = token, Contrasena = contrasena });

    public Task<ResultadoDeLlamada> SolicitarContrasenaAsync(string correo) =>
        EnviarAsync(HttpMethod.Post, "contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = correo });

    public Task<ResultadoDeLlamada> RestablecerContrasenaAsync(string token, string contrasena) =>
        EnviarAsync(HttpMethod.Post, "contrasena/restablecer",
            new PeticionDeRestablecerContrasena { Token = token, Contrasena = contrasena });

    /// <summary>Cambia la contraseña con la sesión iniciada, sabiendo la actual.</summary>
    public Task<ResultadoDeLlamada> CambiarContrasenaAsync(string actual, string nueva) =>
        EnviarAsync(HttpMethod.Put, "yo/contrasena",
            new PeticionDeCambioDeContrasena { ContrasenaActual = actual, ContrasenaNueva = nueva });

    public async Task<(ResultadoDeLlamada Resultado, RespuestaDeInicioDeSesion? Acceso)> IniciarSesionAsync(
        string correo,
        string contrasena)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync("sesiones",
                new PeticionDeInicioDeSesion { Correo = correo, Contrasena = contrasena });

            if (!respuesta.IsSuccessStatusCode)
            {
                return (await TraducirErrorAsync(respuesta), null);
            }

            return (ResultadoDeLlamada.Correcto(),
                await respuesta.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>());
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), null);
        }
    }

    // ------------------------------------------------------------- Recetas

    public Task<(ResultadoDeLlamada Resultado, List<ResumenDeReceta>? Recetas)> ListarRecetasAsync() =>
        LeerAsync<List<ResumenDeReceta>>("recetas");

    /// <param name="raciones">
    /// Comensales a los que ajustar las cantidades. El servidor hace el cálculo y el
    /// redondeo: son reglas de negocio y no se duplican aquí.
    /// </param>
    public Task<(ResultadoDeLlamada Resultado, RespuestaDeReceta? Receta)> ObtenerRecetaAsync(
        Guid id,
        int? raciones = null) =>
        LeerAsync<RespuestaDeReceta>(
            raciones is { } valor ? $"recetas/{id}?raciones={valor}" : $"recetas/{id}");

    public async Task<(ResultadoDeLlamada Resultado, Guid Id)> CrearRecetaAsync(PeticionDeReceta peticion)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync("recetas", peticion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return (await TraducirErrorAsync(respuesta), Guid.Empty);
            }

            var creada = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
            return (ResultadoDeLlamada.Correcto(), creada?.Id ?? Guid.Empty);
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), Guid.Empty);
        }
    }

    /// <summary>
    /// Pide al servidor que lea una receta de una página web. Devuelve un borrador:
    /// no crea nada hasta que el usuario envía el formulario.
    /// </summary>
    public async Task<(ResultadoDeLlamada Resultado, RespuestaDeImportacion? Borrador)> ImportarRecetaAsync(
        string direccion)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync("recetas/importaciones",
                new PeticionDeImportacion { Direccion = direccion });

            if (!respuesta.IsSuccessStatusCode)
            {
                return (await TraducirErrorAsync(respuesta), null);
            }

            return (ResultadoDeLlamada.Correcto(),
                await respuesta.Content.ReadFromJsonAsync<RespuestaDeImportacion>());
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), null);
        }
    }

    public Task<ResultadoDeLlamada> ActualizarRecetaAsync(Guid id, PeticionDeReceta peticion) =>
        EnviarAsync(HttpMethod.Put, $"recetas/{id}", peticion);

    public Task<ResultadoDeLlamada> BorrarRecetaAsync(Guid id) =>
        EnviarAsync(HttpMethod.Delete, $"recetas/{id}");

    // --------------------------------------------------------- Publicación

    public Task<ResultadoDeLlamada> PublicarAsync(Guid id) =>
        EnviarAsync(HttpMethod.Post, $"recetas/{id}/publicacion");

    public Task<ResultadoDeLlamada> DespublicarAsync(Guid id) =>
        EnviarAsync(HttpMethod.Delete, $"recetas/{id}/publicacion");

    // ----------------------------------------------------------- Favoritos

    /// <summary>
    /// Mis favoritas <b>que todavía puedo ver</b>. Lo segundo lo decide el
    /// servidor: una receta marcada y luego despublicada no vuelve en esta lista.
    /// </summary>
    public Task<(ResultadoDeLlamada Resultado, List<ResumenDeReceta>? Recetas)> ListarFavoritasAsync() =>
        LeerAsync<List<ResumenDeReceta>>("recetas/favoritas");

    public Task<ResultadoDeLlamada> MarcarFavoritaAsync(Guid id) =>
        EnviarAsync(HttpMethod.Put, $"recetas/{id}/favorito");

    public Task<ResultadoDeLlamada> DesmarcarFavoritaAsync(Guid id) =>
        EnviarAsync(HttpMethod.Delete, $"recetas/{id}/favorito");

    // -------------------------------------------------------------- Cuenta

    public Task<(ResultadoDeLlamada Resultado, RespuestaDeIdentidad? Identidad)> ObtenerIdentidadAsync() =>
        LeerAsync<RespuestaDeIdentidad>("yo");

    /// <summary>
    /// Descarga el paquete con los datos del usuario.
    /// </summary>
    /// <remarks>
    /// Lo pide este cliente y no un enlace del navegador porque una descarga
    /// iniciada por el navegador no lleva la cabecera de autorización, y meter el
    /// testigo en la dirección lo dejaría escrito en los registros del servidor y
    /// en el historial.
    /// </remarks>
    public async Task<(ResultadoDeLlamada Resultado, Stream? Contenido)> DescargarMisDatosAsync()
    {
        try
        {
            // Sin esperar al cuerpo entero: el paquete puede pesar y no hay razón
            // para tenerlo dos veces en memoria.
            var respuesta = await http.GetAsync(
                "yo/datos",
                HttpCompletionOption.ResponseHeadersRead);

            if (!respuesta.IsSuccessStatusCode)
            {
                return (await TraducirErrorAsync(respuesta), null);
            }

            return (ResultadoDeLlamada.Correcto(), await respuesta.Content.ReadAsStreamAsync());
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), null);
        }
    }

    /// <summary>
    /// Borra la cuenta. La contraseña va en el cuerpo de un DELETE: en la URL
    /// acabaría en los registros del servidor y en el historial del navegador.
    /// </summary>
    public Task<ResultadoDeLlamada> BorrarMiCuentaAsync(string contrasena) =>
        EnviarAsync(HttpMethod.Delete, "yo", new PeticionDeBaja { Contrasena = contrasena });

    // ----------------------------------------------------------- Denuncias

    public Task<ResultadoDeLlamada> DenunciarAsync(Guid recetaId, string motivo, string? comentario) =>
        EnviarAsync(
            HttpMethod.Post,
            $"recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = motivo, Comentario = comentario });

    // --------------------------------------------------------------- Fotos

    public async Task<ResultadoDeLlamada> SubirFotoAsync(Guid recetaId, Stream contenido)
    {
        try
        {
            using var cuerpo = new StreamContent(contenido);
            var respuesta = await http.PostAsync($"recetas/{recetaId}/fotos", cuerpo);

            return respuesta.IsSuccessStatusCode
                ? ResultadoDeLlamada.Correcto()
                : await TraducirErrorAsync(respuesta);
        }
        catch (HttpRequestException)
        {
            return ResultadoDeLlamada.Fallido(MensajeDeRedCaida);
        }
    }

    /// <summary>
    /// Descarga una foto y la devuelve como URL <c>data:</c>, lista para el
    /// atributo <c>src</c> de una imagen.
    /// </summary>
    /// <remarks>
    /// Una etiqueta <c>&lt;img&gt;</c> no manda la cabecera <c>Authorization</c>,
    /// y el endpoint la exige, así que la imagen hay que traerla con este cliente.
    /// <para>
    /// Se devuelve como <c>data:</c> y no como URL de objeto (<c>blob:</c>) porque
    /// la política de seguridad de contenido del sitio permite la primera y no la
    /// segunda; cambiarla exigiría tocar nginx en el servidor. El precio es el
    /// 33 % que engorda base64.
    /// </para>
    /// </remarks>
    public Task<string?> DescargarFotoAsync(Guid recetaId, Guid fotoId) =>
        DescargarImagenAsync($"recetas/{recetaId}/fotos/{fotoId}");

    /// <summary>
    /// Igual, pero la versión reducida. Es la que usan los listados: pintar
    /// cincuenta tarjetas con el archivo original serían cientos de megabytes,
    /// y encima inflados un 33 % por el base64.
    /// </summary>
    public Task<string?> DescargarMiniaturaAsync(Guid recetaId, Guid fotoId) =>
        DescargarImagenAsync($"recetas/{recetaId}/fotos/{fotoId}/miniatura");

    private async Task<string?> DescargarImagenAsync(string ruta)
    {
        try
        {
            var respuesta = await http.GetAsync(ruta);

            if (!respuesta.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            var tipo = respuesta.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            return $"data:{tipo};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<ResultadoDeLlamada> BorrarFotoAsync(Guid recetaId, Guid fotoId) =>
        EnviarAsync(HttpMethod.Delete, $"recetas/{recetaId}/fotos/{fotoId}");

    // ------------------------------------------------------------ Búsqueda

    public Task<(ResultadoDeLlamada Resultado, RespuestaDeBusqueda? Busqueda)> BuscarAsync(
        string? nombre,
        IEnumerable<string> ingredientes,
        string? tipo,
        IEnumerable<string>? etiquetas = null)
    {
        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            partes.Add($"nombre={Uri.EscapeDataString(nombre)}");
        }

        foreach (var ingrediente in ingredientes.Where(valor => !string.IsNullOrWhiteSpace(valor)))
        {
            partes.Add($"ingrediente={Uri.EscapeDataString(ingrediente)}");
        }

        foreach (var etiqueta in (etiquetas ?? []).Where(valor => !string.IsNullOrWhiteSpace(valor)))
        {
            partes.Add($"etiqueta={Uri.EscapeDataString(etiqueta)}");
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            partes.Add($"tipo={Uri.EscapeDataString(tipo)}");
        }

        var consulta = partes.Count > 0 ? "?" + string.Join("&", partes) : string.Empty;

        return LeerAsync<RespuestaDeBusqueda>($"recetas/busqueda{consulta}");
    }

    // ----------------------------------------------------------- Utilidades

    private async Task<(ResultadoDeLlamada Resultado, T? Datos)> LeerAsync<T>(string ruta)
    {
        try
        {
            var respuesta = await http.GetAsync(ruta);

            if (!respuesta.IsSuccessStatusCode)
            {
                return (await TraducirErrorAsync(respuesta), default);
            }

            return (ResultadoDeLlamada.Correcto(), await respuesta.Content.ReadFromJsonAsync<T>());
        }
        catch (HttpRequestException)
        {
            return (ResultadoDeLlamada.Fallido(MensajeDeRedCaida), default);
        }
    }

    /// <summary>Petición sin cuerpo: borrados y cambios de estado.</summary>
    private Task<ResultadoDeLlamada> EnviarAsync(HttpMethod metodo, string ruta) =>
        EnviarAsync<object>(metodo, ruta, null);

    private async Task<ResultadoDeLlamada> EnviarAsync<TPeticion>(
        HttpMethod metodo,
        string ruta,
        TPeticion? contenido)
    {
        try
        {
            using var peticion = new HttpRequestMessage(metodo, ruta);

            if (contenido is not null)
            {
                peticion.Content = JsonContent.Create(contenido);
            }

            var respuesta = await http.SendAsync(peticion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return await TraducirErrorAsync(respuesta);
            }

            // Las respuestas 204 no traen cuerpo; las 200 de esta API traen un
            // mensaje que conviene mostrar.
            if (respuesta.StatusCode == HttpStatusCode.NoContent)
            {
                return ResultadoDeLlamada.Correcto();
            }

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeError>();
            return ResultadoDeLlamada.Correcto(cuerpo?.Mensaje);
        }
        catch (HttpRequestException)
        {
            return ResultadoDeLlamada.Fallido(MensajeDeRedCaida);
        }
        catch (NotSupportedException)
        {
            // Cuerpo que no es JSON en un error inesperado.
            return ResultadoDeLlamada.Fallido("Respuesta inesperada del servidor.");
        }
    }

    private static async Task<ResultadoDeLlamada> TraducirErrorAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return ResultadoDeLlamada.Fallido("Demasiados intentos. Espera unos minutos y vuelve a probar.");
        }

        if (respuesta.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            return ResultadoDeLlamada.Fallido("El archivo es demasiado grande.");
        }

        try
        {
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeError>();

            if (!string.IsNullOrWhiteSpace(cuerpo?.Mensaje))
            {
                return ResultadoDeLlamada.Fallido(cuerpo.Mensaje);
            }
        }
        catch (Exception)
        {
            // La validación automática de ASP.NET responde en otro formato.
        }

        return respuesta.StatusCode switch
        {
            HttpStatusCode.NotFound => ResultadoDeLlamada.Fallido("No se ha encontrado."),
            HttpStatusCode.BadRequest => ResultadoDeLlamada.Fallido("Revisa los datos introducidos."),
            _ => ResultadoDeLlamada.Fallido("No se ha podido completar la operación. Inténtalo de nuevo.")
        };
    }
}

/// <param name="Correcta">Si la operación salió bien.</param>
/// <param name="Mensaje">Texto a mostrar al usuario, ya sea de éxito o de error.</param>
public sealed record ResultadoDeLlamada(bool Correcta, string? Mensaje)
{
    public static ResultadoDeLlamada Correcto(string? mensaje = null) => new(true, mensaje);

    public static ResultadoDeLlamada Fallido(string mensaje) => new(false, mensaje);
}
