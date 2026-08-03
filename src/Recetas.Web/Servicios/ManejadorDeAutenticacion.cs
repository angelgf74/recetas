using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace Recetas.Web.Servicios;

/// <summary>
/// Añade la cabecera <c>Authorization</c> a cada petición y trata el <c>401</c>
/// en un único sitio.
/// </summary>
/// <remarks>
/// <para>
/// Poner la cabecera en cada llamada es justo la clase de cosa que se olvida en
/// el endpoint número once, y olvidarla ahí no da un error de compilación sino un
/// <c>401</c> en producción.
/// </para>
/// <para>
/// El <c>401</c> tampoco es un error de la pantalla que lo recibe: significa que
/// la sesión ya no vale. Se cierra y se lleva al inicio de sesión, y ninguna
/// página tiene que enterarse.
/// </para>
/// </remarks>
public sealed class ManejadorDeAutenticacion(EstadoDeSesion sesion, NavigationManager navegacion)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage peticion,
        CancellationToken cancelacion)
    {
        var token = await sesion.ObtenerTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var respuesta = await base.SendAsync(peticion, cancelacion);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            // Había token y ya no vale: caducado o revocado. Se limpia para que no
            // se siga intentando con él en cada pantalla.
            await sesion.CerrarAsync();
            navegacion.NavigateTo("/sesion?caducada=1", forceLoad: false);
        }

        return respuesta;
    }
}
