using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;
using Recetas.Dominio.Puertos;

namespace Recetas.Api.Tests;

/// <summary>
/// Descargador de pruebas: devuelve el HTML que se le ponga, sin salir a la red.
/// Ningún test debe pedir páginas de verdad.
/// </summary>
public sealed class DescargadorEspia : IDescargadorDePaginas
{
    public string? Html { get; set; }

    /// <summary>Bytes que devuelve <see cref="DescargarImagenAsync"/>, o <c>null</c> para simular un fallo.</summary>
    public byte[]? Imagen { get; set; }

    public Uri? UltimaDireccion { get; private set; }

    public Uri? UltimaImagenPedida { get; private set; }

    public Task<string?> DescargarAsync(Uri direccion, CancellationToken cancelacion = default)
    {
        UltimaDireccion = direccion;
        return Task.FromResult(Html);
    }

    public Task<byte[]?> DescargarImagenAsync(
        Uri direccion, long maximoDeBytes, CancellationToken cancelacion = default)
    {
        UltimaImagenPedida = direccion;
        return Task.FromResult(Imagen);
    }
}

public sealed class ApiConDescargadorEspiaFixture : ApiConPostgresFixture
{
    public DescargadorEspia Descargador { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);

        constructor.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<IDescargadorDePaginas>();
            servicios.AddSingleton<IDescargadorDePaginas>(Descargador);
        });
    }
}

[Trait("Categoria", "Integracion")]
public class ImportacionTests(ApiConDescargadorEspiaFixture api)
    : IClassFixture<ApiConDescargadorEspiaFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    private const string PaginaConReceta =
        """
        <html><head>
        <script type="application/ld+json">
        { "@type": "Recipe", "name": "Fabada asturiana", "recipeYield": "4 raciones",
          "recipeIngredient": ["500 g de fabes", "2 chorizos", "sal al gusto"],
          "recipeInstructions": "Cocer todo a fuego lento." }
        </script>
        </head><body></body></html>
        """;

    [Fact]
    public async Task ConUnaPaginaQuePublicaLaReceta_DevuelveElBorrador()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConReceta;

        var respuesta = await ImportarAsync(cliente, "https://ejemplo.com/fabada");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var borrador = await respuesta.Content.ReadFromJsonAsync<RespuestaDeImportacion>();
        Assert.NotNull(borrador);
        Assert.Equal("Fabada asturiana", borrador.Nombre);
        Assert.Equal(4, borrador.Raciones);
        Assert.Equal(3, borrador.Ingredientes.Count);
        Assert.Equal("https://ejemplo.com/fabada", borrador.Origen);
    }

    /// <summary>
    /// El criterio central: importar es rellenar un formulario, no crear una receta.
    /// Si creara, el recetario se llenaría de extracciones sin revisar.
    /// </summary>
    [Fact]
    public async Task Importar_NoCreaNingunaReceta()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConReceta;

        var antes = await cliente.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");

        await ImportarAsync(cliente, "https://ejemplo.com/fabada");
        await ImportarAsync(cliente, "https://ejemplo.com/fabada");

        var despues = await cliente.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");

        Assert.Equal(antes!.Count, despues!.Count);
    }

    [Fact]
    public async Task ElBorrador_SePuedeGuardarTalCual()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConReceta;

        var borrador = await (await ImportarAsync(cliente, "https://ejemplo.com/fabada"))
            .Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        // Lo que devuelve la importación tiene que valer para crear la receta sin
        // tocar nada: si el dominio lo rechazara, el usuario vería el fallo al
        // guardar y no al importar, que es mucho peor.
        var creada = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = borrador!.Nombre,
            TipoDePlato = "PlatoPrincipal",
            Elaboracion = borrador.Elaboracion,
            Raciones = borrador.Raciones,
            Ingredientes = borrador.Ingredientes.ToList()
        });

        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
    }

    // -------------------------------------------------------------- Imagen (027)

    private const string PaginaConRecetaYFoto =
        """
        <html><head>
        <script type="application/ld+json">
        { "@type": "Recipe", "name": "Fabada asturiana", "recipeYield": "4 raciones",
          "recipeIngredient": ["500 g de fabes", "2 chorizos", "sal al gusto"],
          "recipeInstructions": "Cocer todo a fuego lento.",
          "image": "https://ejemplo.com/fabada.jpg" }
        </script>
        </head><body></body></html>
        """;

    [Fact]
    public async Task ConUnaImagenValida_ElBorradorLaTrae()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConRecetaYFoto;
        api.Descargador.Imagen = ImagenDePrueba.Jpeg();

        var borrador = await (await ImportarAsync(cliente, "https://ejemplo.com/fabada"))
            .Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        Assert.NotNull(borrador!.Imagen);
        Assert.NotEmpty(borrador.Imagen);
    }

    [Fact]
    public async Task LaImagenImportada_SeSubeALaRecetaCreada()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConRecetaYFoto;
        api.Descargador.Imagen = ImagenDePrueba.Jpeg();

        var borrador = await (await ImportarAsync(cliente, "https://ejemplo.com/fabada"))
            .Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        var creada = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = borrador!.Nombre,
            TipoDePlato = "PlatoPrincipal",
            Elaboracion = borrador.Elaboracion,
            Raciones = borrador.Raciones,
            Ingredientes = borrador.Ingredientes.ToList()
        });
        var recetaId = (await creada.Content.ReadFromJsonAsync<RespuestaDeReceta>())!.Id;

        // El paso que hace el cliente: subir los bytes ya traídos por la
        // importación, con el endpoint normal de fotos.
        var subida = await cliente.PostAsync(
            $"/recetas/{recetaId}/fotos", new ByteArrayContent(borrador.Imagen!));
        Assert.Equal(HttpStatusCode.Created, subida.StatusCode);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Single(receta!.Fotos);
    }

    [Fact]
    public async Task SinImagenEnLaPagina_ElBorradorLlegaSinFoto()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConReceta; // sin "image"

        var borrador = await (await ImportarAsync(cliente, "https://ejemplo.com/fabada"))
            .Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        Assert.Null(borrador!.Imagen);
    }

    [Fact]
    public async Task SiLaImagenFalla_LaImportacionSigueSiendoCorrecta()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConRecetaYFoto;
        api.Descargador.Imagen = null; // simula que la descarga falla

        var respuesta = await ImportarAsync(cliente, "https://ejemplo.com/fabada");
        var borrador = await respuesta.Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Fabada asturiana", borrador!.Nombre);
        Assert.Null(borrador.Imagen);
    }

    [Fact]
    public async Task UnaImagenQueNoEsUnFormatoAdmitido_SeIgnora()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = PaginaConRecetaYFoto;
        api.Descargador.Imagen = "esto no es una imagen"u8.ToArray();

        var borrador = await (await ImportarAsync(cliente, "https://ejemplo.com/fabada"))
            .Content.ReadFromJsonAsync<RespuestaDeImportacion>();

        Assert.NotNull(borrador);
        Assert.Null(borrador.Imagen);
    }

    [Fact]
    public async Task UnaPaginaSinReceta_Responde422()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = "<html><body>Un blog cualquiera</body></html>";

        var respuesta = await ImportarAsync(cliente, "https://ejemplo.com/blog");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnaPaginaQueNoSePuedeTraer_Responde502()
    {
        var cliente = await ClienteAutenticadoAsync();
        api.Descargador.Html = null;

        var respuesta = await ImportarAsync(cliente, "https://ejemplo.com/loquesea");

        Assert.Equal(HttpStatusCode.BadGateway, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("esto no es una url")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://ejemplo.com/receta")]
    public async Task UnaDireccionQueNoEsHttp_Responde400(string direccion)
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await ImportarAsync(cliente, direccion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var respuesta = await ImportarAsync(api.CreateClient(), "https://ejemplo.com/fabada");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // ------------------------------------------------------------ Utilidades

    private static Task<HttpResponseMessage> ImportarAsync(HttpClient cliente, string direccion) =>
        cliente.PostAsJsonAsync("/recetas/importaciones", new PeticionDeImportacion { Direccion = direccion });

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"importar-{Guid.NewGuid():N}@ejemplo.com";

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
