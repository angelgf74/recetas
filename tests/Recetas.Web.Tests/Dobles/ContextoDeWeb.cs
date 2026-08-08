using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Recetas.Web.Servicios;

namespace Recetas.Web.Tests.Dobles;

/// <summary>
/// Base de los tests de componentes: deja listos los servicios que la web pide.
/// </summary>
/// <remarks>
/// Hereda de <see cref="TestContext"/> de bUnit, que ya trae un
/// <c>IJSRuntime</c> simulado —el que necesita <see cref="EstadoDeSesion"/> para
/// leer el testigo de <c>localStorage</c>— y un <c>NavigationManager</c> de
/// prueba.
/// </remarks>
public abstract class ContextoDeWeb : TestContext
{
    protected ManejadorDeRespuestas Respuestas { get; } = new();

    protected ContextoDeWeb()
    {
        // bUnit deja el interoperador de JavaScript en modo estricto: cualquier
        // llamada no preparada revienta. Se pasa a laxo porque lo que se prueba
        // aquí es qué se ofrece en pantalla, no el acceso a localStorage.
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Con la sesión cerrada, `RutaProtegida` no pinta nada y todas las
        // páginas renderizarían en blanco. Se simula el testigo guardado en
        // localStorage, que es de donde lo lee `EstadoDeSesion`.
        JSInterop.Setup<string?>("localStorage.getItem", "recetas.acceso")
            .SetResult("testigo-de-prueba");

        Services.AddScoped<EstadoDeSesion>();
        Services.AddScoped(_ => new ClienteDeApi(new HttpClient(Respuestas)
        {
            // Cualquier dirección vale: el manejador no llega a la red. Pero hace
            // falta una base para que las rutas relativas se resuelvan.
            BaseAddress = new Uri("https://api.de-prueba.local/")
        }));
    }
}
