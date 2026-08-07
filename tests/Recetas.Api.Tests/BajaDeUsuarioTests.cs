using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Cuentas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Darse de baja, contra PostgreSQL y con archivos de verdad en disco.
/// </summary>
[Trait("Categoria", "Integracion")]
public class BajaDeUsuarioTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Fact]
    public async Task Baja_ConLaContrasenaCorrecta_DevuelveSinContenido()
    {
        var (cliente, _) = await CuentaAsync();

        var respuesta = await BorrarmeAsync(cliente, Contrasena);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Baja_ConLaContrasenaEquivocada_NoBorraLaCuenta()
    {
        var (cliente, _) = await CuentaAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var respuesta = await BorrarmeAsync(cliente, "no-es-esta-contrasena");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        // La sesión y la receta siguen ahí.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/recetas/{recetaId}")).StatusCode);
    }

    [Fact]
    public async Task Baja_SinSesion_DevuelveNoAutorizado()
    {
        var anonimo = api.CreateClient();

        var respuesta = await BorrarmeAsync(anonimo, Contrasena);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Baja_SeLlevaLasRecetasYSusFotos()
    {
        var (cliente, _) = await CuentaAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var subida = await cliente.PostAsync($"/recetas/{recetaId}/fotos", Contenido(ImagenDePrueba.Jpeg()));
        var foto = await subida.Content.ReadFromJsonAsync<FotoRespuesta>();
        Assert.NotNull(foto);

        // La foto está en disco antes de borrar: sin esta comprobación, el test
        // pasaría igual si nunca hubiera llegado a escribirse.
        var archivos = Directory.GetFiles(api.DirectorioDeFotos, $"{foto.Id:N}*", SearchOption.AllDirectories);
        Assert.NotEmpty(archivos);

        Assert.Equal(HttpStatusCode.NoContent, (await BorrarmeAsync(cliente, Contrasena)).StatusCode);

        // Ninguna cascada de PostgreSQL habría tocado esto.
        Assert.Empty(Directory.GetFiles(api.DirectorioDeFotos, $"{foto.Id:N}*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Baja_DejaElCorreoLibreParaUnaCuentaNueva()
    {
        var (cliente, correo) = await CuentaAsync();
        await BorrarmeAsync(cliente, Contrasena);

        // Alta completa otra vez con la misma dirección.
        var nuevo = await ClienteConCuentaAsync(correo);

        var identidad = await nuevo.GetFromJsonAsync<RespuestaDeIdentidad>("/yo");

        Assert.Equal(correo, identidad!.Correo);

        // Cuenta nueva: no hereda nada de la anterior.
        Assert.Equal(0, identidad.Recetas);
    }

    [Fact]
    public async Task Baja_QuitaSusRecetasPublicasDeLaBusquedaDeLosDemas()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();

        var recetaId = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        Assert.Equal(HttpStatusCode.OK, (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);

        await BorrarmeAsync(ana, Contrasena);

        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);
    }

    [Fact]
    public async Task Yo_DiceCuantoSeVaAPerder()
    {
        var (cliente, _) = await CuentaAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        await cliente.PostAsync($"/recetas/{recetaId}/fotos", Contenido(ImagenDePrueba.Jpeg()));

        var identidad = await cliente.GetFromJsonAsync<RespuestaDeIdentidad>("/yo");

        // Es lo que la pantalla de baja enseña antes de pedir confirmación.
        Assert.Equal(1, identidad!.Recetas);
        Assert.Equal(1, identidad.Fotos);
    }

    [Fact]
    public async Task Baja_AvisaPorCorreoALaDireccionBorrada()
    {
        var (cliente, correo) = await CuentaAsync();

        await BorrarmeAsync(cliente, Contrasena);

        Assert.Contains(correo, api.Correo.ConfirmacionesDeBaja);
    }

    // ------------------------------------------------------------- Auxiliares

    /// <summary>
    /// El cuerpo viaja en un DELETE, que es lo poco habitual de este endpoint:
    /// <c>HttpClient.DeleteAsync</c> no admite contenido, así que se arma la
    /// petición a mano.
    /// </summary>
    private static Task<HttpResponseMessage> BorrarmeAsync(HttpClient cliente, string contrasena)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Delete, "/yo")
        {
            Content = JsonContent.Create(new PeticionDeBaja { Contrasena = contrasena })
        };

        return cliente.SendAsync(peticion);
    }

    private static ByteArrayContent Contenido(byte[] bytes)
    {
        var contenido = new ByteArrayContent(bytes);
        contenido.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return contenido;
    }

    private static async Task<Guid> CrearRecetaAsync(HttpClient cliente)
    {
        var respuesta = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = "Tortilla de patatas",
            TipoDePlato = "PlatoPrincipal",
            Elaboracion = "Batir, freír, cuajar.",
            Ingredientes = [new LineaDeIngredientePeticion("Patata", 500m, "Gramo")]
        });

        var receta = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        return receta!.Id;
    }

    private async Task<(HttpClient Cliente, string Correo)> CuentaAsync()
    {
        var correo = $"baja-{Guid.NewGuid():N}@ejemplo.com";

        return (await ClienteConCuentaAsync(correo), correo);
    }

    private async Task<HttpClient> ClienteConCuentaAsync(string correo)
    {
        var cliente = api.CreateClient();

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = Contrasena
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = Contrasena });

        Assert.True(
            sesion.StatusCode == HttpStatusCode.OK,
            $"No se pudo preparar el usuario de prueba: /sesiones respondió {(int)sesion.StatusCode}.");

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        Assert.NotNull(acceso);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);

        return cliente;
    }
}
