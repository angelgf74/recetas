using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;
using Recetas.Infraestructura.Persistencia;

namespace Recetas.Api.Tests;

/// <summary>
/// Ciclo completo del recetario privado contra PostgreSQL real, con especial
/// atención a que un usuario no alcance las recetas de otro.
/// </summary>
[Trait("Categoria", "Integracion")]
public class RecetasTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    private static PeticionDeReceta Peticion(
        string nombre = "Tortilla de patatas",
        params LineaDeIngredientePeticion[] ingredientes) =>
        new()
        {
            Nombre = nombre,
            TipoDePlato = "PlatoPrincipal",
            Elaboracion = "Batir los huevos, freír la patata, cuajar.",
            Ingredientes = ingredientes.Length > 0
                ? [.. ingredientes]
                : [new LineaDeIngredientePeticion("Patata", 500m, "Gramo")]
        };

    // ---------------------------------------------------------------- Ciclo

    [Fact]
    public async Task CicloCompleto_CrearLeerEditarBorrar()
    {
        var cliente = await ClienteAutenticadoAsync();

        var creacion = await cliente.PostAsJsonAsync("/recetas", Peticion());
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);

        var creada = await creacion.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        Assert.NotNull(creada);
        Assert.Equal("Privada", creada.Visibilidad);
        Assert.Equal(creacion.Headers.Location?.ToString(), $"/recetas/{creada.Id}");

        var lectura = await cliente.GetAsync($"/recetas/{creada.Id}");
        Assert.Equal(HttpStatusCode.OK, lectura.StatusCode);
        var leida = await lectura.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        Assert.Equal("patata", leida!.Ingredientes.Single().Nombre);

        var edicion = await cliente.PutAsJsonAsync($"/recetas/{creada.Id}",
            Peticion("Tortilla con cebolla", new LineaDeIngredientePeticion("Cebolla", 1m, "Unidad")));
        Assert.Equal(HttpStatusCode.NoContent, edicion.StatusCode);

        var reLectura = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{creada.Id}");
        Assert.Equal("Tortilla con cebolla", reLectura!.Nombre);
        Assert.Equal("cebolla", reLectura.Ingredientes.Single().Nombre);

        var borrado = await cliente.DeleteAsync($"/recetas/{creada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/recetas/{creada.Id}")).StatusCode);
    }

    // ---------------------------------------------------------- Aislamiento

    [Fact]
    public async Task UnUsuarioNoAlcanzaLasRecetasDeOtro()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var creada = await (await ana.PostAsJsonAsync("/recetas", Peticion("Secreta de Ana")))
            .Content.ReadFromJsonAsync<RespuestaDeReceta>();

        // Las tres operaciones responden 404, no 403: un 403 confirmaría que existe.
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/recetas/{creada!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bruno.PutAsJsonAsync($"/recetas/{creada.Id}", Peticion("Secuestrada"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.DeleteAsync($"/recetas/{creada.Id}")).StatusCode);

        // Y sigue intacta para su autora.
        var deAna = await ana.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{creada.Id}");
        Assert.Equal("Secreta de Ana", deAna!.Nombre);
    }

    [Fact]
    public async Task RecetaAjenaYRecetaInexistente_RespondenIgual()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var creada = await (await ana.PostAsJsonAsync("/recetas", Peticion()))
            .Content.ReadFromJsonAsync<RespuestaDeReceta>();

        var ajena = await bruno.GetAsync($"/recetas/{creada!.Id}");
        var inexistente = await bruno.GetAsync($"/recetas/{Guid.NewGuid()}");

        Assert.Equal(ajena.StatusCode, inexistente.StatusCode);
        Assert.Equal(
            await ajena.Content.ReadAsStringAsync(),
            await inexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Listar_SoloDevuelveLasPropias()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        await ana.PostAsJsonAsync("/recetas", Peticion("De Ana 1"));
        await ana.PostAsJsonAsync("/recetas", Peticion("De Ana 2"));
        await bruno.PostAsJsonAsync("/recetas", Peticion("De Bruno"));

        var deAna = await ana.GetFromJsonAsync<List<ResumenDeReceta>>("/recetas");

        Assert.Equal(2, deAna!.Count);
        Assert.DoesNotContain(deAna, receta => receta.Nombre == "De Bruno");
    }

    [Fact]
    public async Task SinToken_TodoResponde401()
    {
        var anonimo = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/recetas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.PostAsJsonAsync("/recetas", Peticion())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.GetAsync($"/recetas/{Guid.NewGuid()}")).StatusCode);
    }

    // ------------------------------------------------------------ Validación

    [Fact]
    public async Task TipoDePlatoFueraDeLaLista_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();
        var peticion = Peticion();
        peticion.TipoDePlato = "Ensalada";

        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/recetas", peticion)).StatusCode);
    }

    [Fact]
    public async Task IngredienteRepetido_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();

        var peticion = Peticion("Con repetidos",
            new LineaDeIngredientePeticion("Tomate", 2m, "Unidad"),
            new LineaDeIngredientePeticion("  TOMATE ", 3m, "Unidad"));

        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/recetas", peticion)).StatusCode);
    }

    // ------------------------------------------------------------ Etiquetas

    [Fact]
    public async Task Etiquetas_IdaYVueltaPorLaApi()
    {
        var cliente = await ClienteAutenticadoAsync();

        var peticion = Peticion();
        peticion.Etiquetas = ["Rápido", "Sin gluten"];

        var creada = await (await cliente.PostAsJsonAsync("/recetas", peticion))
            .Content.ReadFromJsonAsync<RespuestaDeReceta>();

        // Ya en minúsculas, como el resto de nombres del catálogo.
        Assert.Equal(["rápido", "sin gluten"], creada!.Etiquetas!.OrderBy(e => e, StringComparer.Ordinal));

        var editada = Peticion("Con menos etiquetas");
        editada.Etiquetas = ["Rápido"];

        await cliente.PutAsJsonAsync($"/recetas/{creada.Id}", editada);

        var reLeida = await cliente.GetFromJsonAsync<RespuestaDeReceta>($"/recetas/{creada.Id}");
        Assert.Equal(["rápido"], reLeida!.Etiquetas);
    }

    [Fact]
    public async Task SinEtiquetas_EsValido()
    {
        var cliente = await ClienteAutenticadoAsync();

        var creacion = await cliente.PostAsJsonAsync("/recetas", Peticion());

        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        var creada = await creacion.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        Assert.Empty(creada!.Etiquetas!);
    }

    [Fact]
    public async Task MasEtiquetasQueElTope_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();
        var peticion = Peticion();
        peticion.Etiquetas = Enumerable.Range(0, PeticionDeReceta.MaximoDeEtiquetas + 1)
            .Select(numero => $"etiqueta-{numero}")
            .ToList();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/recetas", peticion)).StatusCode);
    }

    // -------------------------------------------------------- Persistencia

    [Fact]
    public async Task Borrar_NoDejaLineasHuerfanas()
    {
        var cliente = await ClienteAutenticadoAsync();

        var creada = await (await cliente.PostAsJsonAsync("/recetas",
                Peticion("Para borrar",
                    new LineaDeIngredientePeticion("Alcaparra", 10m, "Gramo"),
                    new LineaDeIngredientePeticion("Eneldo", null, "AlGusto"))))
            .Content.ReadFromJsonAsync<RespuestaDeReceta>();

        await cliente.DeleteAsync($"/recetas/{creada!.Id}");

        using var ambito = api.Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<RecetasDbContext>();

        var lineas = await contexto.Set<Dominio.Recetas.IngredienteDeReceta>()
            .Where(linea => linea.RecetaId == creada.Id)
            .CountAsync();

        Assert.Equal(0, lineas);
    }

    [Fact]
    public async Task ElCatalogo_NoDuplicaIngredientesConElMismoNombreNormalizado()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        await ana.PostAsJsonAsync("/recetas",
            Peticion("Una", new LineaDeIngredientePeticion("Pimentón  Dulce", 1m, "Cucharadita")));
        await bruno.PostAsJsonAsync("/recetas",
            Peticion("Otra", new LineaDeIngredientePeticion("  PIMENTÓN DULCE ", 2m, "Cucharadita")));

        using var ambito = api.Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<RecetasDbContext>();

        // Igual que en el repositorio: se compara el objeto valor completo, no
        // `Nombre.Valor`, que EF no sabe traducir a SQL.
        var pimenton = Dominio.Recetas.NombreDeIngrediente.Crear("pimentón dulce");

        var cuantos = await contexto.Ingredientes
            .CountAsync(ingrediente => ingrediente.Nombre == pimenton);

        Assert.Equal(1, cuantos);
    }

    // ------------------------------------------------------------- Utilidades

    /// <summary>Da de alta un usuario nuevo y devuelve un cliente con su token.</summary>
    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"usuario-{Guid.NewGuid():N}@ejemplo.com";

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = Contrasena
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = Contrasena });

        // Se comprueba el estado aquí, y no solo se desreferencia el token: si el
        // alta o el login fallan (por ejemplo, al agotar el límite de peticiones),
        // el error aparecería como un token nulo dentro del test que toque, sin
        // ninguna pista de que el problema estaba en la preparación.
        Assert.True(
            sesion.StatusCode == HttpStatusCode.OK,
            $"No se pudo preparar el usuario de prueba: /sesiones respondió {(int)sesion.StatusCode}.");

        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        Assert.NotNull(acceso);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);

        return cliente;
    }
}
