using Microsoft.JSInterop;

namespace Recetas.Web.Servicios;

/// <summary>
/// Guarda si hay sesión abierta y con qué token.
/// </summary>
/// <remarks>
/// El token vive en <c>localStorage</c> para que la sesión sobreviva a una
/// recarga. Es una elección con contrapartida, ya anotada en la feature 002: una
/// cookie <c>HttpOnly</c> resistiría mejor a XSS, pero WebAssembly no podría
/// leerla para componer la cabecera <c>Authorization</c>. Se compensa no
/// inyectando nunca HTML sin escapar.
/// </remarks>
public sealed class EstadoDeSesion(IJSRuntime js)
{
    private const string Clave = "recetas.acceso";

    private string? _token;
    private bool _leido;

    /// <summary>Se dispara al abrir o cerrar sesión, para que la interfaz se repinte.</summary>
    public event Action? Cambio;

    public bool HaySesion => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Token actual, leyéndolo de <c>localStorage</c> la primera vez.
    /// </summary>
    /// <remarks>
    /// La lectura es asíncrona y no puede hacerse en el constructor, así que se
    /// hace aquí y se recuerda. Sin este paso, recargar la página parecería haber
    /// cerrado la sesión.
    /// </remarks>
    public async ValueTask<string?> ObtenerTokenAsync()
    {
        if (_leido)
        {
            return _token;
        }

        try
        {
            _token = await js.InvokeAsync<string?>("localStorage.getItem", Clave);
        }
        catch (JSException)
        {
            // Algunos navegadores bloquean localStorage. Se sigue sin sesión en
            // lugar de dejar la aplicación inservible.
            _token = null;
        }

        _leido = true;
        return _token;
    }

    public async Task AbrirAsync(string token)
    {
        _token = token;
        _leido = true;

        await js.InvokeVoidAsync("localStorage.setItem", Clave, token);
        Cambio?.Invoke();
    }

    public async Task CerrarAsync()
    {
        _token = null;
        _leido = true;

        await js.InvokeVoidAsync("localStorage.removeItem", Clave);
        Cambio?.Invoke();
    }
}
