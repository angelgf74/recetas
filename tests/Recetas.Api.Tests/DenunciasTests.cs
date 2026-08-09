using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Moderacion;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Denunciar una receta pública y retirarla.
/// </summary>
[Trait("Categoria", "Integracion")]
public class DenunciasTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    /// <summary>
    /// Correos de cuenta fija ya dados de alta. xUnit crea una instancia por test,
    /// pero la base de datos del fixture es la misma para toda la clase.
    /// </summary>
    private static readonly HashSet<string> CuentasCreadas = [];

    // ------------------------------------------------------------- Denunciar

    [Fact]
    public async Task Denunciar_UnaPublicaAjena_DevuelveSinContenido()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var respuesta = await bruno.PostAsJsonAsync($"/recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = "Spam", Comentario = "Es publicidad." });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Denunciar_SinSesion_DevuelveNoAutorizado()
    {
        var ana = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var anonimo = api.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync($"/recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = "Spam" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Denunciar_UnaPrivadaAjena_DevuelveNoEncontrada()
    {
        // Un 403 confirmaría que existe. Misma regla que en toda la API.
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(ana);

        var respuesta = await bruno.PostAsJsonAsync($"/recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = "Ofensivo" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Denunciar_LaPropia_DevuelvePeticionIncorrecta()
    {
        var ana = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var respuesta = await ana.PostAsJsonAsync($"/recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = "Otro" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Denunciar_ConMotivoInventado_DevuelvePeticionIncorrecta()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var respuesta = await bruno.PostAsJsonAsync($"/recetas/{recetaId}/denuncias",
            new PeticionDeDenuncia { Motivo = "MeCaeMal" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Denunciar_DosVeces_SigueRespondiendoBien()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var cuerpo = new PeticionDeDenuncia { Motivo = "Spam" };

        Assert.Equal(HttpStatusCode.NoContent,
            (await bruno.PostAsJsonAsync($"/recetas/{recetaId}/denuncias", cuerpo)).StatusCode);

        // La segunda no crea nada, pero al usuario se le responde igual: para él
        // la receta ya está denunciada.
        Assert.Equal(HttpStatusCode.NoContent,
            (await bruno.PostAsJsonAsync($"/recetas/{recetaId}/denuncias", cuerpo)).StatusCode);
    }

    // --------------------------------------------------------------- Retirar

    [Fact]
    public async Task Retirar_ElResponsablePuedeDespublicarUnaAjena()
    {
        var ana = await ClienteAutenticadoAsync();
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(ana);

        var retirada = await responsable.DeleteAsync($"/recetas/{recetaId}/publicacion");

        Assert.Equal(HttpStatusCode.NoContent, retirada.StatusCode);

        // Retirar no borra: su autora la conserva, ahora privada.
        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Privada", paraAna!.Visibilidad);
    }

    [Fact]
    public async Task Retirar_AvisaPorCorreoAlAutor()
    {
        var correoDeAna = $"ana-{Guid.NewGuid():N}@ejemplo.com";
        var ana = await ClienteAutenticadoAsync(correoDeAna);
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(ana);

        await responsable.DeleteAsync($"/recetas/{recetaId}/publicacion");

        // Sin aviso, la receta desaparece de la vista sin que su autora sepa
        // por qué ni pueda corregirla. Es la feature 020 entera.
        var aviso = Assert.Single(api.Correo.AvisosDeRetirada, envio => envio.Destinatario == correoDeAna);
        Assert.Equal("Tortilla de patatas", aviso.Receta);
    }

    [Fact]
    public async Task Despublicar_ElAutorNoRecibeAvisoDeRetirada()
    {
        // Quitar de público lo propio es una decisión, no una moderación:
        // avisar sería mandarse un correo a uno mismo por cada clic.
        var correoDeAna = $"ana-{Guid.NewGuid():N}@ejemplo.com";
        var ana = await ClienteAutenticadoAsync(correoDeAna);
        var recetaId = await CrearPublicaAsync(ana);

        var respuesta = await ana.DeleteAsync($"/recetas/{recetaId}/publicacion");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.DoesNotContain(api.Correo.AvisosDeRetirada, envio => envio.Destinatario == correoDeAna);
    }

    [Fact]
    public async Task Despublicar_ElResponsableSobreLoPropio_NoSeAutoavisa()
    {
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(responsable);

        var respuesta = await responsable.DeleteAsync($"/recetas/{recetaId}/publicacion");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.DoesNotContain(
            api.Correo.AvisosDeRetirada,
            envio => envio.Destinatario == ApiConPostgresFixture.CorreoDelResponsable);
    }

    [Fact]
    public async Task LaFicha_DiceSiQuienPreguntaPuedeRetirarla()
    {
        // Sin este campo la interfaz no puede ofrecer la retirada, y el endpoint
        // existe pero no hay forma de llegar a él desde la aplicación. Pasó:
        // la primera versión de la 015 dejó al responsable sin botón.
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(ana);

        var paraElResponsable = await responsable.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        var paraBruno = await bruno.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");

        Assert.True(paraElResponsable!.PuedoRetirarla);
        Assert.False(paraBruno!.PuedoRetirarla);

        // Sobre la propia, no: para eso ya están las acciones de autor.
        Assert.False(paraAna!.PuedoRetirarla);
    }

    [Fact]
    public async Task LaFicha_NoOfreceRetirarLoQueYaEsPrivado()
    {
        var ana = await ClienteAutenticadoAsync();
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(ana);

        await responsable.DeleteAsync($"/recetas/{recetaId}/publicacion");

        // Ya retirada: el responsable no la ve, así que tampoco hay nada que
        // ofrecer. Y la autora la conserva, sin oferta de retirada.
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await responsable.GetAsync($"/recetas/{recetaId}")).StatusCode);

        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.False(paraAna!.PuedoRetirarla);
    }

    [Fact]
    public async Task Retirar_UnUsuarioCualquiera_NoPuede()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var recetaId = await CrearPublicaAsync(ana);

        var intento = await bruno.DeleteAsync($"/recetas/{recetaId}/publicacion");

        Assert.Equal(HttpStatusCode.NotFound, intento.StatusCode);

        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Publica", paraAna!.Visibilidad);
    }

    [Fact]
    public async Task Retirar_ElResponsableNoPuedeEditarNiBorrarRecetasAjenas()
    {
        // Moderar es retirar de la vista, y nada más. Si esto se relajara, el
        // responsable pasaría a poder reescribir recetas de cualquiera.
        var ana = await ClienteAutenticadoAsync();
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearPublicaAsync(ana);

        var edicion = await responsable.PutAsJsonAsync($"/recetas/{recetaId}", new PeticionDeReceta
        {
            Nombre = "Secuestrada",
            TipoDePlato = "Postre",
            Elaboracion = "Otros pasos.",
            Ingredientes = [new LineaDeIngredientePeticion("Azúcar", 100m, "Gramo")]
        });

        var borrado = await responsable.DeleteAsync($"/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.NotFound, edicion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, borrado.StatusCode);

        var paraAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Tortilla de patatas", paraAna!.Nombre);
    }

    [Fact]
    public async Task Retirar_ElResponsableNoPuedeVerRecetasPrivadasAjenas()
    {
        var ana = await ClienteAutenticadoAsync();
        var responsable = await ClienteAutenticadoAsync(ApiConPostgresFixture.CorreoDelResponsable);
        var recetaId = await CrearRecetaAsync(ana);

        var lectura = await responsable.GetAsync($"/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.NotFound, lectura.StatusCode);
    }

    // ------------------------------------------------------------- Auxiliares

    private async Task<Guid> CrearRecetaAsync(HttpClient cliente)
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

    private async Task<Guid> CrearPublicaAsync(HttpClient cliente)
    {
        var recetaId = await CrearRecetaAsync(cliente);
        await cliente.PostAsync($"/recetas/{recetaId}/publicacion", null);

        return recetaId;
    }

    /// <summary>
    /// Cliente con sesión iniciada. Con <paramref name="correoFijo"/> la cuenta se
    /// reutiliza si ya existe: la del responsable la comparten varios tests, y el
    /// alta solo funciona una vez por correo.
    /// </summary>
    private async Task<HttpClient> ClienteAutenticadoAsync(string? correoFijo = null)
    {
        var cliente = api.CreateClient();
        var correo = correoFijo ?? $"denuncia-{Guid.NewGuid():N}@ejemplo.com";

        var yaExiste = correoFijo is not null && !CuentasCreadas.Add(correoFijo);

        if (!yaExiste)
        {
            await cliente.PostAsJsonAsync("/registro/solicitudes",
                new PeticionDeSolicitudDeRegistro { Correo = correo });

            await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
            {
                Token = api.Correo.TokenEnviadoA(correo),
                Contrasena = Contrasena
            });
        }

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
