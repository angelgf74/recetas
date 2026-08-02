using Microsoft.JSInterop;

namespace Recetas.Web.Servicios;

/// <summary>
/// Guarda el token de acceso en <c>localStorage</c> para que la sesión sobreviva
/// a una recarga.
/// </summary>
/// <remarks>
/// Elección con contrapartida: una cookie <c>HttpOnly</c> sería inaccesible desde
/// JavaScript y por tanto más resistente a XSS, pero entonces WebAssembly no podría
/// leerla para componer la cabecera <c>Authorization</c>. Se compensa no inyectando
/// nunca HTML sin escapar en la web.
/// </remarks>
public sealed class AlmacenDeSesion(IJSRuntime js)
{
    private const string Clave = "recetas.acceso";

    public async Task GuardarAsync(string token) =>
        await js.InvokeVoidAsync("localStorage.setItem", Clave, token);

    public async Task<string?> LeerAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", Clave);

    public async Task BorrarAsync() =>
        await js.InvokeVoidAsync("localStorage.removeItem", Clave);
}
