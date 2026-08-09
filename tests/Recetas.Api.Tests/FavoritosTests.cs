using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Cuentas;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Favoritos privados contra la base de datos real: es aquí donde se comprueba la
/// consulta que une marcas y recetas, y las cascadas de borrado.
/// </summary>
[Trait("Categoria", "Integracion")]
public class FavoritosTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Fact]
    public async Task Marcar_UnaPublicaAjena_ApareceEnMisFavoritas()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");

        var marcada = await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        Assert.Equal(HttpStatusCode.NoContent, marcada.StatusCode);

        var favoritas = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas");
        Assert.Contains(favoritas!, receta => receta.Id == recetaId);

        var ficha = await bruno.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.True(ficha!.EsFavorita);
    }

    [Fact]
    public async Task Marcar_DosVeces_RespondeIgual()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");

        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);
        var segunda = await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);

        var favoritas = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas");
        Assert.Single(favoritas!, receta => receta.Id == recetaId);
    }

    [Fact]
    public async Task Marcar_UnaPrivadaAjena_DevuelveNoEncontrada()
    {
        // Un 403 confirmaría que existe. Misma regla que en toda la API.
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearRecetaAsync(ana, "Tortilla de patatas");

        var respuesta = await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Empty((await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas"))!);
    }

    [Fact]
    public async Task Desmarcar_LoQueNoEstabaMarcado_DevuelveSinContenido()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");

        var respuesta = await bruno.DeleteAsync($"/recetas/{recetaId}/favorito");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Favoritas_NoDevuelveLoQueDejoDeEstarPublicado()
    {
        // Sin este filtro, marcar una receta antes de que la retiren sería una
        // forma de seguir viéndola. Va contra la base de datos porque la consulta
        // que lo garantiza es de EF, no del caso de uso.
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");
        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        await ana.DeleteAsync($"/recetas/{recetaId}/publicacion");

        var mientrasEstaPrivada = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas");
        Assert.DoesNotContain(mientrasEstaPrivada!, receta => receta.Id == recetaId);

        // Y tampoco la ficha, claro.
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await bruno.GetAsync($"/recetas/{recetaId}")).StatusCode);

        // La marca no se perdió: al volver a publicarse, vuelve a la lista.
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        var alRepublicar = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas");
        Assert.Contains(alRepublicar!, receta => receta.Id == recetaId);
    }

    [Fact]
    public async Task Favoritas_SonDeCadaUno()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");

        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        // Lo que marca Bruno no es asunto de Ana, ni siquiera siendo la autora: ni
        // en su lista, ni en la ficha, ni como recuento en ninguna parte.
        Assert.Empty((await ana.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas"))!);

        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.False(paraAna!.EsFavorita);
    }

    [Fact]
    public async Task LaFicha_NoDiceCuantasVecesSeHaMarcado()
    {
        // El hueco es deliberado: un recuento haría competir a las recetas entre
        // sí, que es justo lo que mission.md descarta al rechazar las
        // valoraciones. Si alguien añade el campo, este test lo para.
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");
        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        var crudo = await ana.GetStringAsync($"/recetas/{recetaId}");

        Assert.DoesNotContain("favoritos", crudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("veces", crudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Borrar_LaReceta_SeLlevaLaMarca()
    {
        // Si la cascada no estuviera, la fila quedaría apuntando a una receta que
        // ya no existe y la consulta de favoritos fallaría al unir.
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");
        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        await ana.DeleteAsync($"/recetas/{recetaId}");

        var favoritas = await bruno.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas/favoritas");
        Assert.DoesNotContain(favoritas!, receta => receta.Id == recetaId);
    }

    [Fact]
    public async Task Baja_SeLlevaLosFavoritosDeQuienSeVa()
    {
        var (ana, _) = await CuentaAsync();
        var (bruno, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");
        await bruno.PutAsync($"/recetas/{recetaId}/favorito", null);

        var baja = await bruno.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/yo")
        {
            Content = JsonContent.Create(new PeticionDeBaja { Contrasena = Contrasena })
        });

        Assert.Equal(HttpStatusCode.NoContent, baja.StatusCode);

        // La receta de Ana sigue viva; lo que se fue son las marcas de Bruno. Si
        // la cascada faltara, la baja habría fallado con un error de clave ajena.
        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Tortilla de patatas", paraAna!.Nombre);
    }

    [Fact]
    public async Task Favoritos_SinSesion_DevuelveNoAutorizado()
    {
        var (ana, _) = await CuentaAsync();
        var recetaId = await CrearPublicaAsync(ana, "Tortilla de patatas");
        var anonimo = api.CreateClient();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonimo.GetAsync("/recetas/favoritas")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonimo.PutAsync($"/recetas/{recetaId}/favorito", null)).StatusCode);
    }

    // ------------------------------------------------------------- Auxiliares

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

    private static async Task<Guid> CrearPublicaAsync(HttpClient cliente, string nombre)
    {
        var recetaId = await CrearRecetaAsync(cliente, nombre);
        await cliente.PostAsync($"/recetas/{recetaId}/publicacion", null);

        return recetaId;
    }

    private async Task<(HttpClient Cliente, string Correo)> CuentaAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"favorito-{Guid.NewGuid():N}@ejemplo.com";

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
