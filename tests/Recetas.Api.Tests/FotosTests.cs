using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Fotos de receta contra PostgreSQL y disco reales.
/// </summary>
[Trait("Categoria", "Integracion")]
public class FotosTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    // Imágenes reales, no cabeceras inventadas: desde la feature 005 el servidor
    // decodifica lo que sube para limpiarle los metadatos.
    private static byte[] Jpeg() => ImagenDePrueba.Jpeg();

    private static byte[] Png() => ImagenDePrueba.Png();

    // ---------------------------------------------------------------- Ciclo

    [Fact]
    public async Task CicloCompleto_SubirDescargarBorrar()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var subida = await SubirAsync(cliente, recetaId, Jpeg());
        Assert.Equal(HttpStatusCode.Created, subida.StatusCode);

        var foto = await subida.Content.ReadFromJsonAsync<FotoRespuesta>();
        Assert.NotNull(foto);
        Assert.Equal("Jpeg", foto.Tipo);

        // El archivo está de verdad en el disco, no en la base de datos.
        Assert.True(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{foto.Id:N}.jpg")));

        var descarga = await cliente.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("image/jpeg", descarga.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", descarga.Headers.GetValues("X-Content-Type-Options").Single());

        // Los bytes NO coinciden con los subidos, y debe ser así: el servidor
        // recodifica la imagen para quitarle los metadatos (feature 005). Lo que
        // se comprueba es que lo servido sigue siendo una imagen válida y con las
        // mismas dimensiones.
        var descargados = await descarga.Content.ReadAsByteArrayAsync();
        using var imagen = SixLabors.ImageSharp.Image.Load(descargados);
        Assert.Equal(8, imagen.Width);
        Assert.Equal(8, imagen.Height);

        // La ficha de la receta la incluye.
        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Single(receta!.Fotos);

        var borrado = await cliente.DeleteAsync($"/recetas/{recetaId}/fotos/{foto.Id}");
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);
        Assert.False(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{foto.Id:N}.jpg")));
        Assert.Equal(HttpStatusCode.NotFound,
            (await cliente.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);
    }

    // ----------------------------------------------------------- Aislamiento

    [Fact]
    public async Task UnUsuarioNoVeNiBorraLaFotoDeOtro()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var recetaId = await CrearRecetaAsync(ana);
        var foto = await (await SubirAsync(ana, recetaId, Jpeg())).Content.ReadFromJsonAsync<FotoRespuesta>();

        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.GetAsync($"/recetas/{recetaId}/fotos/{foto!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.DeleteAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SubirAsync(bruno, recetaId, Png())).StatusCode);

        // Sigue intacta para su dueña.
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync($"/recetas/{recetaId}/fotos/{foto.Id}")).StatusCode);
    }

    [Fact]
    public async Task SinToken_TodoResponde401()
    {
        var anonimo = api.CreateClient();
        var recetaId = Guid.NewGuid();
        var fotoId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.GetAsync($"/recetas/{recetaId}/fotos/{fotoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await SubirAsync(anonimo, recetaId, Jpeg())).StatusCode);
    }

    // ------------------------------------------------------------ Validación

    [Fact]
    public async Task ArchivoQueNoEsImagen_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        // Declarado como imagen a propósito: el servidor debe mirar los bytes.
        var contenido = new ByteArrayContent("<html><script>alert(1)</script></html>"u8.ToArray());
        contenido.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var respuesta = await cliente.PostAsync($"/recetas/{recetaId}/fotos", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(Directory.GetFiles(api.DirectorioDeFotos));
    }

    [Fact]
    public async Task ArchivoVacio_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var respuesta = await cliente.PostAsync($"/recetas/{recetaId}/fotos", new ByteArrayContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // -------------------------------------------------------------- Portada

    [Fact]
    public async Task ElegirPortada_CambiaCualEsPortadaEnLaFicha()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var primera = await (await SubirAsync(cliente, recetaId, Jpeg())).Content
            .ReadFromJsonAsync<FotoRespuesta>();
        var segunda = await (await SubirAsync(cliente, recetaId, Png())).Content
            .ReadFromJsonAsync<FotoRespuesta>();

        // Sin elegir nada, sigue siendo la más antigua: el comportamiento de la
        // 009 no cambia para quien no toca nada.
        var antesDeElegir = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.True(antesDeElegir!.Fotos.Single(f => f.Id == primera!.Id).EsPortada);

        var eleccion = await cliente.PutAsync($"/recetas/{recetaId}/fotos/{segunda!.Id}/portada", null);
        Assert.Equal(HttpStatusCode.NoContent, eleccion.StatusCode);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.True(receta!.Fotos.Single(f => f.Id == segunda.Id).EsPortada);
        Assert.False(receta.Fotos.Single(f => f.Id == primera!.Id).EsPortada);
    }

    [Fact]
    public async Task BorrarLaPortadaElegida_VuelveALaDerivada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var primera = await (await SubirAsync(cliente, recetaId, Jpeg())).Content
            .ReadFromJsonAsync<FotoRespuesta>();
        var segunda = await (await SubirAsync(cliente, recetaId, Png())).Content
            .ReadFromJsonAsync<FotoRespuesta>();

        await cliente.PutAsync($"/recetas/{recetaId}/fotos/{segunda!.Id}/portada", null);
        await cliente.DeleteAsync($"/recetas/{recetaId}/fotos/{segunda.Id}");

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.True(receta!.Fotos.Single(f => f.Id == primera!.Id).EsPortada);
    }

    [Fact]
    public async Task UnUsuarioCualquiera_NoPuedeElegirPortadaAjena()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var recetaId = await CrearRecetaAsync(ana);
        var foto = await (await SubirAsync(ana, recetaId, Jpeg())).Content.ReadFromJsonAsync<FotoRespuesta>();

        var respuesta = await bruno.PutAsync($"/recetas/{recetaId}/fotos/{foto!.Id}/portada", null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElegirUnaFotoDeOtraReceta_Responde404()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaUno = await CrearRecetaAsync(cliente);
        var recetaDos = await CrearRecetaAsync(cliente);

        var fotoDeLaUno = await (await SubirAsync(cliente, recetaUno, Jpeg())).Content
            .ReadFromJsonAsync<FotoRespuesta>();

        var respuesta = await cliente.PutAsync($"/recetas/{recetaDos}/fotos/{fotoDeLaUno!.Id}/portada", null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // ------------------------------------------------------------ Persistencia

    [Fact]
    public async Task BorrarLaReceta_NoDejaArchivosHuerfanos()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente);

        var una = await (await SubirAsync(cliente, recetaId, Jpeg())).Content.ReadFromJsonAsync<FotoRespuesta>();
        var otra = await (await SubirAsync(cliente, recetaId, Png())).Content.ReadFromJsonAsync<FotoRespuesta>();

        Assert.True(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{una!.Id:N}.jpg")));
        Assert.True(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{otra!.Id:N}.png")));

        await cliente.DeleteAsync($"/recetas/{recetaId}");

        // La cascada de la base de datos no toca el disco: si el caso de uso no
        // borrara los archivos, estos seguirían ahí sin ninguna fila que los cite.
        Assert.False(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{una.Id:N}.jpg")));
        Assert.False(File.Exists(Path.Combine(api.DirectorioDeFotos, $"{otra.Id:N}.png")));
    }

    // ------------------------------------------------------------- Utilidades

    private static Task<HttpResponseMessage> SubirAsync(HttpClient cliente, Guid recetaId, byte[] bytes) =>
        cliente.PostAsync($"/recetas/{recetaId}/fotos", new ByteArrayContent(bytes));

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
        var correo = $"foto-{Guid.NewGuid():N}@ejemplo.com";

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
