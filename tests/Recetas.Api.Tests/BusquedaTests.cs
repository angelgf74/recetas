using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Recetas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Búsqueda multicriterio contra PostgreSQL real.
/// </summary>
[Trait("Categoria", "Integracion")]
public class BusquedaTests(ApiConPostgresFixture api) : IClassFixture<ApiConPostgresFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    // ------------------------------------------------------------ Criterios

    [Fact]
    public async Task Busca_PorNombreParcial()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Tortilla de patatas", "PlatoPrincipal", ("Patata", "Gramo"));
        await CrearAsync(cliente, "Gazpacho", "PrimerPlato", ("Tomate", "Unidad"));

        var resultado = await BuscarAsync(cliente, "nombre=tortilla");

        Assert.Single(resultado.Resultados);
        Assert.Equal("Tortilla de patatas", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task Busca_SinImportarMayusculasNiAcentos()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Jamón con Pimentón", "Entrante", ("Jamón", "Gramo"));

        // Escrito sin acentos y en mayúsculas, como lo teclea cualquiera.
        var resultado = await BuscarAsync(cliente, "nombre=JAMON");

        Assert.Single(resultado.Resultados);
        // El nombre que se devuelve conserva sus acentos: normalizar es solo para buscar.
        Assert.Equal("Jamón con Pimentón", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task Busca_PorIngredienteSinAcentos()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Patatas bravas", "Entrante", ("Pimentón", "Cucharadita"));
        await CrearAsync(cliente, "Ensaladilla", "Entrante", ("Atún", "Gramo"));

        var resultado = await BuscarAsync(cliente, "ingrediente=pimenton");

        Assert.Single(resultado.Resultados);
        Assert.Equal("Patatas bravas", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task VariosIngredientes_ExigenQueEstenTodos()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Con los dos", "PlatoPrincipal", ("Tomate", "Unidad"), ("Albahaca", "Hoja"));
        await CrearAsync(cliente, "Solo tomate", "PlatoPrincipal", ("Tomate", "Unidad"));

        var resultado = await BuscarAsync(cliente, "ingrediente=tomate&ingrediente=albahaca");

        // Con O saldrían las dos, y cuantos más ingredientes se pidieran más
        // resultados habría: lo contrario de afinar.
        Assert.Single(resultado.Resultados);
        Assert.Equal("Con los dos", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task Busca_PorEtiquetaSinAcentos()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearConEtiquetasAsync(cliente, "Con etiqueta", ["Rápido"]);
        await CrearConEtiquetasAsync(cliente, "Sin ella", ["Lento"]);

        var resultado = await BuscarAsync(cliente, "etiqueta=rapido");

        Assert.Single(resultado.Resultados);
        Assert.Equal("Con etiqueta", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task VariasEtiquetas_ExigenQueEstenTodas()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearConEtiquetasAsync(cliente, "Con las dos", ["Rápido", "Sin gluten"]);
        await CrearConEtiquetasAsync(cliente, "Solo una", ["Rápido"]);

        var resultado = await BuscarAsync(cliente, $"etiqueta=rapido&etiqueta={Uri.EscapeDataString("sin gluten")}");

        Assert.Single(resultado.Resultados);
        Assert.Equal("Con las dos", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task Etiquetas_DeUnaPrivadaAjena_NoFiltranNada()
    {
        // El filtro de visibilidad va antes que el de etiqueta en la misma
        // consulta: buscar por una etiqueta que solo lleva una receta privada
        // ajena no debe encontrarla, o la búsqueda serviría para averiguar qué
        // etiquetas usa otra persona en lo que no comparte.
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();
        var etiquetaUnica = $"secreta-{Guid.NewGuid():N}";

        await CrearConEtiquetasAsync(ana, "Privada de Ana", [etiquetaUnica]);

        var resultado = await BuscarAsync(bruno, $"etiqueta={etiquetaUnica}");

        Assert.Empty(resultado.Resultados);
    }

    [Fact]
    public async Task Busca_PorTipoDePlato()
    {
        var cliente = await ClienteAutenticadoAsync();
        var postre = $"Flan {Guid.NewGuid():N}";
        var primero = $"Sopa {Guid.NewGuid():N}";

        await CrearAsync(cliente, postre, "Postre", ("Huevo", "Unidad"));
        await CrearAsync(cliente, primero, "PrimerPlato", ("Fideo", "Gramo"));

        var resultado = await BuscarAsync(cliente, "tipo=Postre");
        var nombres = resultado.Resultados.Select(receta => receta.Nombre).ToList();

        // Se comprueba qué entra y qué no, no cuántos hay: otros tests dejan
        // recetas publicadas, y una receta pública es visible para todos por
        // diseño. Contar sería frágil por una razón que no tiene que ver con el
        // criterio que se está probando.
        Assert.Contains(postre, nombres);
        Assert.DoesNotContain(primero, nombres);
    }

    [Fact]
    public async Task LosCriterios_SeCombinan()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Tarta de queso", "Postre", ("Queso", "Gramo"));
        await CrearAsync(cliente, "Tarta salada", "PlatoPrincipal", ("Queso", "Gramo"));
        await CrearAsync(cliente, "Tarta de manzana", "Postre", ("Manzana", "Unidad"));

        var resultado = await BuscarAsync(cliente, "nombre=tarta&tipo=Postre&ingrediente=queso");

        Assert.Single(resultado.Resultados);
        Assert.Equal("Tarta de queso", resultado.Resultados.Single().Nombre);
    }

    [Fact]
    public async Task TipoDePlatoInexistente_Responde400()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync("/recetas/busqueda?tipo=Ensalada");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ----------------------------------------------------------- Visibilidad

    [Fact]
    public async Task NoEncuentra_LasPrivadasDeOtroUsuario()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var nombreUnico = $"Secreta {Guid.NewGuid():N}";
        await CrearAsync(ana, nombreUnico, "Postre", ("Azúcar", "Gramo"));

        // Buscando su nombre exacto, que es el peor caso.
        var resultado = await BuscarAsync(bruno, $"nombre={Uri.EscapeDataString(nombreUnico)}");

        Assert.Empty(resultado.Resultados);
    }

    [Fact]
    public async Task Encuentra_LasPublicasDeOtroUsuario()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var nombreUnico = $"Compartida {Guid.NewGuid():N}";
        var recetaId = await CrearAsync(ana, nombreUnico, "Postre", ("Azúcar", "Gramo"));
        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);

        var resultado = await BuscarAsync(bruno, $"nombre={Uri.EscapeDataString(nombreUnico)}");

        var encontrada = Assert.Single(resultado.Resultados);
        Assert.Equal(nombreUnico, encontrada.Nombre);
        // Bruno sabe que no es suya, pero no de quién es.
        Assert.False(encontrada.EsMia);
    }

    [Fact]
    public async Task Encuentra_LasPropiasAunqueSeanPrivadas()
    {
        var ana = await ClienteAutenticadoAsync();
        var nombreUnico = $"Mia {Guid.NewGuid():N}";
        await CrearAsync(ana, nombreUnico, "Postre", ("Azúcar", "Gramo"));

        var resultado = await BuscarAsync(ana, $"nombre={Uri.EscapeDataString(nombreUnico)}");

        var encontrada = Assert.Single(resultado.Resultados);
        Assert.True(encontrada.EsMia);
        Assert.Equal("Privada", encontrada.Visibilidad);
    }

    [Fact]
    public async Task Despublicar_LaSacaDeLosResultadosAjenos()
    {
        var ana = await ClienteAutenticadoAsync();
        var bruno = await ClienteAutenticadoAsync();

        var nombreUnico = $"Temporal {Guid.NewGuid():N}";
        var recetaId = await CrearAsync(ana, nombreUnico, "Postre", ("Azúcar", "Gramo"));
        var consulta = $"nombre={Uri.EscapeDataString(nombreUnico)}";

        await ana.PostAsync($"/recetas/{recetaId}/publicacion", null);
        Assert.Single((await BuscarAsync(bruno, consulta)).Resultados);

        await ana.DeleteAsync($"/recetas/{recetaId}/publicacion");
        Assert.Empty((await BuscarAsync(bruno, consulta)).Resultados);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var respuesta = await api.CreateClient().GetAsync("/recetas/busqueda?nombre=tortilla");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // --------------------------------------------------------------- Límites

    [Fact]
    public async Task SinCriterios_DevuelveLoVisibleSinFiltrar()
    {
        var cliente = await ClienteAutenticadoAsync();
        await CrearAsync(cliente, "Una cualquiera", "Postre", ("Azúcar", "Gramo"));

        var resultado = await BuscarAsync(cliente, string.Empty);

        Assert.NotEmpty(resultado.Resultados);
    }

    // ------------------------------------------------------------- Utilidades

    private static async Task<RespuestaDeBusqueda> BuscarAsync(HttpClient cliente, string consulta)
    {
        var url = consulta.Length > 0 ? $"/recetas/busqueda?{consulta}" : "/recetas/busqueda";
        var respuesta = await cliente.GetFromJsonAsync<RespuestaDeBusqueda>(url);

        return respuesta!;
    }

    private static async Task<Guid> CrearAsync(
        HttpClient cliente,
        string nombre,
        string tipo,
        params (string Nombre, string Unidad)[] ingredientes)
    {
        var respuesta = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = nombre,
            TipoDePlato = tipo,
            Elaboracion = "Pasos de la receta.",
            Ingredientes = ingredientes
                .Select(par => new LineaDeIngredientePeticion(
                    par.Nombre,
                    par.Unidad == "AlGusto" ? null : 1m,
                    par.Unidad))
                .ToList()
        });

        var receta = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        return receta!.Id;
    }

    private static async Task<Guid> CrearConEtiquetasAsync(
        HttpClient cliente, string nombre, List<string> etiquetas)
    {
        var respuesta = await cliente.PostAsJsonAsync("/recetas", new PeticionDeReceta
        {
            Nombre = nombre,
            TipoDePlato = "PlatoPrincipal",
            Elaboracion = "Pasos de la receta.",
            Ingredientes = [new LineaDeIngredientePeticion("Patata", 1m, "Unidad")],
            Etiquetas = etiquetas
        });

        var receta = await respuesta.Content.ReadFromJsonAsync<RespuestaDeReceta>();
        return receta!.Id;
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = api.CreateClient();
        var correo = $"busca-{Guid.NewGuid():N}@ejemplo.com";

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
