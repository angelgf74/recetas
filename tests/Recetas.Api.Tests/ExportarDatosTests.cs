using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Recetas.Contratos.Cuentas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Exportar los datos: qué se lleva el usuario y qué no.
/// </summary>
[Trait("Categoria", "Integracion")]
public class ExportarDatosTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Fact]
    public async Task Exportar_SinSesion_DevuelveNoAutorizado()
    {
        var anonimo = api.CreateClient();

        var respuesta = await anonimo.GetAsync("/yo/datos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Exportar_DevuelveUnZipConLasRecetasYSusFotos()
    {
        var (cliente, correo) = await CuentaAsync();
        var recetaId = await CrearRecetaAsync(cliente, "Tortilla de patatas");
        await cliente.PostAsync($"/recetas/{recetaId}/fotos", Imagen());

        using var paquete = await DescargarAsync(cliente);

        // Las tres piezas que promete la spec.
        Assert.NotNull(paquete.GetEntry("datos.json"));
        Assert.NotNull(paquete.GetEntry("LEEME.txt"));
        Assert.Single(paquete.Entries.Where(e => e.FullName.StartsWith("fotos/")));

        var datos = await LeerDatosAsync(paquete);

        Assert.Equal(correo, datos.Correo);

        var receta = Assert.Single(datos.Recetas);
        Assert.Equal("Tortilla de patatas", receta.Nombre);
        Assert.Equal("PlatoPrincipal", receta.TipoDePlato);
        Assert.Equal("Batir, freír, cuajar.", receta.Elaboracion);
        Assert.Single(receta.Ingredientes);
        Assert.Equal("patata", receta.Ingredientes[0].Nombre);

        // El JSON dice qué archivo del paquete es la foto de esta receta, y ese
        // archivo existe de verdad.
        var nombreDeLaFoto = Assert.Single(receta.Fotos);
        Assert.NotNull(paquete.GetEntry(nombreDeLaFoto));
    }

    [Fact]
    public async Task Exportar_NoIncluyeNadaDeOtrosUsuarios()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();

        // Bruno publica la suya: Ana puede verla, pero no es dato suyo.
        var deBruno = await CrearRecetaAsync(bruno, "Gazpacho de Bruno");
        await bruno.PostAsync($"/recetas/{deBruno}/publicacion", null);
        await bruno.PostAsync($"/recetas/{deBruno}/fotos", Imagen());

        await CrearRecetaAsync(ana, "Tortilla de Ana");

        using var paquete = await DescargarAsync(ana);
        var datos = await LeerDatosAsync(paquete);

        Assert.Single(datos.Recetas);
        Assert.Equal("Tortilla de Ana", datos.Recetas[0].Nombre);
        Assert.DoesNotContain(datos.Recetas, receta => receta.Nombre.Contains("Bruno"));

        // Y tampoco la foto de Bruno, que es lo que se colaría si la exportación
        // preguntara por lo visible en vez de por lo propio.
        Assert.Empty(paquete.Entries.Where(e => e.FullName.StartsWith("fotos/")));
    }

    [Fact]
    public async Task Exportar_UnaCuentaSinRecetas_DevuelveUnArchivoValido()
    {
        var (cliente, correo) = await CuentaAsync();

        using var paquete = await DescargarAsync(cliente);
        var datos = await LeerDatosAsync(paquete);

        Assert.Equal(correo, datos.Correo);
        Assert.Empty(datos.Recetas);
        Assert.NotNull(paquete.GetEntry("LEEME.txt"));
    }

    [Fact]
    public async Task Exportar_NoFiltraElHashDeLaContrasena()
    {
        var (cliente, _) = await CuentaAsync();
        await CrearRecetaAsync(cliente, "Tortilla de patatas");

        using var paquete = await DescargarAsync(cliente);

        await using var entrada = paquete.GetEntry("datos.json")!.Open();
        using var lector = new StreamReader(entrada);
        var contenido = await lector.ReadToEndAsync();

        // El formato del hasheador del proyecto. Si apareciera, el paquete que el
        // usuario deja en su carpeta de descargas llevaría dentro su credencial.
        Assert.DoesNotContain("pbkdf2", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Contrasena, contenido);
    }

    // ------------------------------------------------------------- Auxiliares

    private static async Task<ZipArchive> DescargarAsync(HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync("/yo/datos");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/zip", respuesta.Content.Headers.ContentType?.MediaType);

        // A memoria dentro del test: ZipArchive necesita poder buscar, y el flujo
        // de la respuesta no lo permite.
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();

        return new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
    }

    private static async Task<DatosExportados> LeerDatosAsync(ZipArchive paquete)
    {
        await using var entrada = paquete.GetEntry("datos.json")!.Open();

        var datos = await JsonSerializer.DeserializeAsync<DatosExportados>(
            entrada,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(datos);

        return datos;
    }

    private static ByteArrayContent Imagen()
    {
        var contenido = new ByteArrayContent(ImagenDePrueba.Jpeg());
        contenido.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return contenido;
    }

    private static async Task<Guid> CrearRecetaAsync(HttpClient cliente, string nombre)
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

    private async Task<(HttpClient Cliente, string Correo)> CuentaAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"exporta-{Guid.NewGuid():N}@ejemplo.com";

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = Contrasena
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = Contrasena });

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        Assert.NotNull(acceso);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);

        return (cliente, correo);
    }
}
