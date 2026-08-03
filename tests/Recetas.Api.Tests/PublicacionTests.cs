using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Publicar y despublicar, y lo que eso abre y lo que NO abre.
/// </summary>
[Trait("Categoria", "Integracion")]
public class PublicacionTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    private static byte[] Jpeg() => ImagenDePrueba.Jpeg();

    // ------------------------------------------------------------- Publicar

    [Fact]
    public async Task Publicar_HaceLaRecetaLegibleParaOtroUsuario()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        // Antes de publicar, Bruno no la ve.
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);

        var publicacion = await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.NoContent, publicacion.StatusCode);

        var leida = await bruno.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Publica", leida!.Visibilidad);
    }

    [Fact]
    public async Task Despublicar_LaVuelveAOcultar()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.OK, (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);

        await ana.DeleteAsync($"/recetas/{recetaId}/publicacion");

        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);
    }

    [Fact]
    public async Task Publicar_EsIdempotente()
    {
        var ana = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        Assert.Equal(HttpStatusCode.NoContent,
            (await ana.PostAsync($"/recetas/{recetaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await ana.PostAsync($"/recetas/{recetaId}/publicacion", null)).StatusCode);
    }

    [Fact]
    public async Task PublicarLaRecetaDeOtro_Responde404()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.PostAsync($"/recetas/{recetaId}/publicacion", null)).StatusCode);
    }

    [Fact]
    public async Task DespublicarLaRecetaPublicaDeOtro_Responde404()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        // Bruno la ve, pero no puede retirarla del escaparate de Ana.
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.DeleteAsync($"/recetas/{recetaId}/publicacion")).StatusCode);
    }

    // ------------------------------------- Ver no es tener: el riesgo central

    [Fact]
    public async Task UnaRecetaPublica_NoSePuedeEditarNiBorrarPorOtro()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        var edicion = await bruno.PutAsJsonAsync($"/recetas/{recetaId}", new PeticionDeReceta
        {
            Nombre = "Secuestrada",
            TipoDePlato = "Postre",
            Elaboracion = "Pasos ajenos",
            Ingredientes = [new LineaDeIngredientePeticion("Azúcar", 1m, "Cucharada")]
        });

        Assert.Equal(HttpStatusCode.NotFound, edicion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.DeleteAsync($"/recetas/{recetaId}")).StatusCode);

        // Intacta.
        var receta = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Tortilla de patatas", receta!.Nombre);
    }

    [Fact]
    public async Task UnaRecetaPrivadaAjena_SigueSiendoInvisible()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var privada = await CrearRecetaAsync(ana);
        var publica = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{publica}/publicacion", null);

        // Publicar una no destapa las demás.
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/recetas/{privada}")).StatusCode);
    }

    [Fact]
    public async Task Listar_SigueDevolviendoSoloLasPropias()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        // El recetario personal no es un muro: explorar lo publicado es la 006.
        var deBruno = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");
        Assert.Empty(deBruno!);
    }

    // ---------------------------------------------------------------- Fotos

    [Fact]
    public async Task LasFotosSiguenLaVisibilidadDeSuReceta()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        var subida = await ana.PostAsync($"/recetas/{recetaId}/fotos", new ByteArrayContent(Jpeg()));
        var foto = await subida.Content.ReadFromJsonAsync<FotoRespuesta>();

        // Privada: la foto tampoco.
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.GetAsync($"/recetas/{recetaId}/fotos/{foto!.Id}")).StatusCode);

        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        // Publicada: la foto sí.
        Assert.Equal(HttpStatusCode.OK,
            (await bruno.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);

        // Pero sigue sin poder borrarla ni añadir las suyas.
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.DeleteAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.PostAsync($"/recetas/{recetaId}/fotos", new ByteArrayContent(Jpeg()))).StatusCode);
    }

    // ------------------------------------------------------------ Anonimato

    [Fact]
    public async Task SinToken_NiSiquieraLoPublicoEsLegible()
    {
        var ana = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        var anonimo = api.CreateClient();

        // "Público" significa "visible para quien tenga cuenta", nunca "abierto a
        // internet". Es el principio de comunidad cerrada de mission.md.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/recetas/{recetaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.PostAsync($"/recetas/{recetaId}/publicacion", null)).StatusCode);
    }

    // ------------------------------------------------------------- Utilidades

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

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"publica-{Guid.NewGuid():N}@ejemplo.com";

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
