using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recetas.Web;
using Recetas.Web.Servicios;

var constructor = WebAssemblyHostBuilder.CreateDefault(args);
constructor.RootComponents.Add<App>("#app");
constructor.RootComponents.Add<HeadOutlet>("head::after");

// La API vive en otro origen que la web: se configura en wwwroot/appsettings.json
// para poder apuntar a producción sin recompilar.
var baseDeLaApi = constructor.Configuration["Api:Base"] ?? "http://localhost:5199/";

constructor.Services.AddScoped<EstadoDeSesion>();
constructor.Services.AddScoped<ManejadorDeAutenticacion>();

// El HttpClient pasa por el manejador que añade la cabecera Authorization y
// centraliza el 401. Ninguna página construye peticiones a mano.
constructor.Services.AddScoped(proveedor =>
{
    var manejador = proveedor.GetRequiredService<ManejadorDeAutenticacion>();
    manejador.InnerHandler = new HttpClientHandler();

    return new HttpClient(manejador) { BaseAddress = new Uri(baseDeLaApi) };
});

constructor.Services.AddScoped<ClienteDeApi>();

await constructor.Build().RunAsync();
