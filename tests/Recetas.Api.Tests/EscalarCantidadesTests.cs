using System.Net;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Escalado por comensales contra PostgreSQL real: el parámetro de consulta, el
/// rango, y sobre todo que escalar no escribe nada.
/// </summary>
[Trait("Categoria", "Integracion")]
public class EscalarCantidadesTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    // ------------------------------------------------------------- Escalar

    [Fact]
    public async Task ConRaciones_DevuelveLasCantidadesAjustadas()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}?raciones=8");

        Assert.NotNull(receta);
        Assert.Equal(4, receta.Raciones);
        Assert.Equal(8, receta.RacionesMostradas);
        Assert.Equal(600m, CantidadDe(receta, "harina"));
        Assert.Equal(4m, CantidadDe(receta, "huevo"));
    }

    // ------------------------------------------------- Conversión de unidad

    [Fact]
    public async Task AlCruzarMilGramos_ConvierteAKilogramos()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        // 300 g × 4 = 1200 g de harina.
        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}?raciones=16");

        Assert.NotNull(receta);
        Assert.Equal("Kilogramo", UnidadDe(receta, "harina"));
        Assert.Equal(1.25m, CantidadDe(receta, "harina"));
    }

    [Fact]
    public async Task SinPedirRaciones_NoConvierteAunqueElValorGuardadoSuperaraElUmbral()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        // Se sube a mano a 1200 g, editando directamente: la unidad guardada
        // sigue siendo gramo.
        await cliente.PutAsJsonAsync($"/recetas/{recetaId}", new PeticionDeReceta
        {
            Nombre = "Tortitas",
            TipoDePlato = "Postre",
            Elaboracion = "Mezclar y cuajar.",
            Raciones = 4,
            Ingredientes =
            [
                new LineaDeIngredientePeticion("harina", 1200m, "Gramo"),
                new LineaDeIngredientePeticion("huevo", 2m, "Unidad")
            ]
        });

        // La ficha en reposo y lo que precarga la edición piden esto mismo, sin
        // el parámetro: la unidad guardada llega intacta.
        var sinParametro = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Equal("Gramo", UnidadDe(sinParametro!, "harina"));
        Assert.Equal(1200m, CantidadDe(sinParametro, "harina"));
    }

    [Fact]
    public async Task SinElParametro_LlegaTalComoEstaGuardada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");

        Assert.NotNull(receta);
        Assert.Equal(4, receta.RacionesMostradas);
        Assert.Equal(300m, CantidadDe(receta, "harina"));
    }

    [Fact]
    public async Task ConLasMismasRaciones_DevuelveLasCantidadesExactas()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}?raciones=4");

        Assert.NotNull(receta);
        Assert.Equal(300m, CantidadDe(receta, "harina"));
        Assert.Equal(2m, CantidadDe(receta, "huevo"));
    }

    /// <summary>
    /// El criterio central: escalar es leer. Si escribiera, la receta quedaría
    /// corrompida por el simple hecho de consultarla con otro número de comensales.
    /// </summary>
    [Fact]
    public async Task Escalar_NoModificaLaRecetaGuardada()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        await cliente.GetAsync($"/recetas/{recetaId}?raciones=40");
        await cliente.GetAsync($"/recetas/{recetaId}?raciones=1");

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");

        Assert.NotNull(receta);
        Assert.Equal(300m, CantidadDe(receta, "harina"));
        Assert.Equal(2m, CantidadDe(receta, "huevo"));
        Assert.Equal(4, receta.Raciones);
    }

    [Fact]
    public async Task SobreUnaRecetaSinRaciones_SeIgnoraSinError()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: null);

        var respuesta = await cliente.GetAsync($"/recetas/{recetaId}?raciones=8");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var receta = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        Assert.Null(receta!.Raciones);
        Assert.Null(receta.RacionesMostradas);
        Assert.Equal(300m, CantidadDe(receta, "harina"));
    }

    [Fact]
    public async Task AlGusto_SigueSinCantidadAlEscalar()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}?raciones=12");

        Assert.Null(CantidadDe(receta!, "sal"));
    }

    // -------------------------------------------------------------- Rango

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public async Task RacionesFueraDeRango_Responde400(int raciones)
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var respuesta = await cliente.GetAsync($"/recetas/{recetaId}?raciones={raciones}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearConRacionesFueraDeRango_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsJsonAsync("/recetas", PeticionDe(raciones: 500));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ---------------------------------------------------------- Persistencia

    [Fact]
    public async Task LasRaciones_SeGuardanYSeDevuelven()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 6);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");

        Assert.Equal(6, receta!.Raciones);
    }

    [Fact]
    public async Task Editar_PuedeQuitarLasRaciones()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: 4);

        var actualizar = await cliente.PutAsJsonAsync($"/recetas/{recetaId}", PeticionDe(raciones: null));
        Assert.Equal(HttpStatusCode.NoContent, actualizar.StatusCode);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");
        Assert.Null(receta!.Raciones);
    }

    [Fact]
    public async Task UnaRecetaSinRaciones_LasDevuelveNulas()
    {
        var cliente = await ClienteAutenticadoAsync();
        var recetaId = await CrearRecetaAsync(cliente, raciones: null);

        var receta = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{recetaId}");

        Assert.Null(receta!.Raciones);
        Assert.Null(receta.RacionesMostradas);
    }

    // ------------------------------------------------------------ Utilidades

    private static decimal? CantidadDe(RespuestaDeReceta receta, string ingrediente) =>
        receta.Ingredientes.Single(linea => linea.Nombre == ingrediente).Cantidad;

    private static string UnidadDe(RespuestaDeReceta receta, string ingrediente) =>
        receta.Ingredientes.Single(linea => linea.Nombre == ingrediente).Unidad;

    /// <summary>Para 4: 300 g de harina, 2 huevos y sal al gusto.</summary>
    private static PeticionDeReceta PeticionDe(int? raciones) => new()
    {
        Nombre = "Tortitas",
        TipoDePlato = "Postre",
        Elaboracion = "Mezclar y cuajar.",
        Raciones = raciones,
        Ingredientes =
        [
            new LineaDeIngredientePeticion("harina", 300m, "Gramo"),
            new LineaDeIngredientePeticion("huevo", 2m, "Unidad"),
            new LineaDeIngredientePeticion("sal", null, "AlGusto")
        ]
    };

    private static async Task<Guid> CrearRecetaAsync(HttpClient cliente, int? raciones)
    {
        var respuesta = await cliente.PostAsJsonAsync("/recetas", PeticionDe(raciones));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var receta = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        return receta!.Id;
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"raciones-{Guid.NewGuid():N}@ejemplo.com";

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
