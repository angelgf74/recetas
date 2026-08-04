using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Miniaturas contra PostgreSQL y disco reales: generación, visibilidad
/// heredada, generación perezosa y portada en los listados.
/// </summary>
[Trait("Categoria", "Integracion")]
public class MiniaturasTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Fact]
    public async Task Subir_DejaEnDiscoElOriginalYLaMiniatura()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        var foto = await SubirFotoAsync(cliente, recetaId);

        Assert.True(File.Exists(Original(foto.Id)));
        Assert.True(File.Exists(Miniatura(foto.Id)));
    }

    [Fact]
    public async Task Descargar_DevuelveUnaImagenMasPequenaQueElOriginal()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        // Grande de verdad: con la imagen de 8×8 del resto de tests no habría nada
        // que reducir y este test no diría nada.
        var foto = await SubirFotoAsync(cliente, recetaId, ImagenDePrueba.Jpeg(1200, 900));

        var respuesta = await cliente.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}/miniatura");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("image/jpeg", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());

        using var miniatura = SixLabors.ImageSharp.Image.Load(
            await respuesta.Content.ReadAsByteArrayAsync());

        Assert.Equal(320, miniatura.Width);
        Assert.Equal(240, miniatura.Height);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var respuesta = await api.CreateClient()
            .GetAsync($"/recetas/{Guid.NewGuid()}/fotos/{Guid.NewGuid()}/miniatura");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>
    /// Un `403` confirmaría que esa receta existe. La miniatura sigue la misma
    /// regla que la foto completa.
    /// </summary>
    [Fact]
    public async Task DeRecetaPrivadaAjena_Responde404()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var recetaId = await CrearRecetaAsync(ana);
        var foto = await SubirFotoAsync(ana, recetaId);

        var respuesta = await bruno.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}/miniatura");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        // Y para su dueña sigue funcionando: sin esto, el 404 podría venir de que
        // la miniatura no existiera en absoluto.
        Assert.Equal(HttpStatusCode.OK,
            (await ana.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}/miniatura")).StatusCode);
    }

    [Fact]
    public async Task DeRecetaPublicadaAjena_SeSirve()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var recetaId = await CrearRecetaAsync(ana);
        var foto = await SubirFotoAsync(ana, recetaId);

        Assert.Equal(HttpStatusCode.NoContent,
            (await ana.PostAsync($"/recetas/{recetaId}/publicacion", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await bruno.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}/miniatura")).StatusCode);
    }

    /// <summary>
    /// Es el caso de las fotos subidas antes de la feature 009: están en disco sin
    /// miniatura. Se simula borrando el archivo de la miniatura a mano.
    /// </summary>
    [Fact]
    public async Task SinMiniaturaEnDisco_LaGeneraAlPedirla()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        var foto = await SubirFotoAsync(cliente, recetaId);

        File.Delete(Miniatura(foto.Id));
        Assert.False(File.Exists(Miniatura(foto.Id)));

        var respuesta = await cliente.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}/miniatura");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(File.Exists(Miniatura(foto.Id)), "La miniatura no se ha guardado al generarla.");
    }

    [Fact]
    public async Task Borrar_SeLlevaLosDosArchivos()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        var foto = await SubirFotoAsync(cliente, recetaId);

        Assert.Equal(HttpStatusCode.NoContent,
            (await cliente.DeleteAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);

        Assert.False(File.Exists(Original(foto.Id)));
        Assert.False(File.Exists(Miniatura(foto.Id)), "La miniatura ha sobrevivido a su foto.");
    }

    [Fact]
    public async Task BorrarLaReceta_TampocoDejaMiniaturasHuerfanas()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        var foto = await SubirFotoAsync(cliente, recetaId);

        await cliente.DeleteAsync($"/recetas/{recetaId}");

        Assert.False(File.Exists(Original(foto.Id)));
        Assert.False(File.Exists(Miniatura(foto.Id)));
    }

    // -------------------------------------------------------------- Portada

    [Fact]
    public async Task ElRecetario_DiceCualEsLaPortada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);
        var primera = await SubirFotoAsync(cliente, recetaId);
        await SubirFotoAsync(cliente, recetaId);

        var listado = await cliente.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");
        var resumen = listado!.Single(receta => receta.Id == recetaId);

        // La primera que se subió, no la última.
        Assert.Equal(primera.Id, resumen.FotoDePortadaId);
    }

    [Fact]
    public async Task UnaRecetaSinFotos_NoTienePortada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var listado = await cliente.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");

        Assert.Null(listado!.Single(receta => receta.Id == recetaId).FotoDePortadaId);
    }

    [Fact]
    public async Task LaBusqueda_TambienTraeLaPortada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, "Bizcocho de miniaturas");
        var foto = await SubirFotoAsync(cliente, recetaId);

        var busqueda = await cliente.GetFromJsonAsync<RespuestaDeBusqueda>(
            "/recetas/busqueda?nombre=bizcocho%20de%20miniaturas");

        var resumen = busqueda!.Resultados.Single(receta => receta.Id == recetaId);
        Assert.Equal(foto.Id, resumen.FotoDePortadaId);
    }

    // ------------------------------------------------------------ Utilidades

    private string Original(Guid fotoId) => Path.Combine(api.DirectorioDeFotos, $"{fotoId:N}.jpg");

    private string Miniatura(Guid fotoId) => Path.Combine(api.DirectorioDeFotos, $"{fotoId:N}-min.jpg");

    private static async Task<FotoRespuesta> SubirFotoAsync(
        HttpClient cliente,
        Guid recetaId,
        byte[]? bytes = null)
    {
        var respuesta = await cliente.PostAsync(
            $"/recetas/{recetaId}/fotos",
            new ByteArrayContent(bytes ?? ImagenDePrueba.Jpeg()));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var foto = await respuesta.Content.ReadFromJsonAsync<FotoRespuesta>();
        Assert.NotNull(foto);

        return foto;
    }

    private static async Task<Guid> CrearRecetaAsync(HttpClient cliente, string nombre = "Tortilla")
    {
        var respuesta = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = nombre,
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
        var correo = $"miniatura-{Guid.NewGuid():N}@ejemplo.com";

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
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", acceso!.Token);

        return cliente;
    }
}
