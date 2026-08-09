using Recetas.Aplicacion.Favoritos;
using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Favoritos;

/// <summary>
/// Favoritos privados: qué se puede marcar, qué se ve después y qué pasa cuando
/// la receta deja de estar publicada.
/// </summary>
public class GestionDeFavoritosTests
{
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly RepositorioDeFavoritosEnMemoria _favoritos = new();
    private readonly AlmacenDeFotosEnMemoria _almacen = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));
    private readonly RepositorioDeRecetasEnMemoria _recetas;

    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bruno = Guid.NewGuid();

    public GestionDeFavoritosTests() =>
        _recetas = new RepositorioDeRecetasEnMemoria(_ingredientes, _favoritos);

    private GestionDeFavoritos Favoritos => new(_recetas, _favoritos, _reloj);

    private GestionDeRecetas RecetasDe =>
        new(_recetas, new ResolverIngredientes(_ingredientes), new ResolverEtiquetas(new RepositorioDeEtiquetasEnMemoria()), _almacen, _reloj);

    [Fact]
    public async Task Marcar_UnaPublicaAjena_ApareceEnMiLista()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        var resultado = await Favoritos.MarcarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeFavorito.Correcto, resultado);

        var mias = await Favoritos.ListarMisFavoritasAsync(_bruno);
        Assert.Equal(recetaId, Assert.Single(mias).Id);
    }

    [Fact]
    public async Task Marcar_LaPropia_TambienVale()
    {
        // La regla es "lo que puedas ver", sin excepción para lo propio: en un
        // recetario grande, señalar las que salen bien es el mismo caso de uso.
        var recetaId = await CrearRecetaAsync(_ana);

        var resultado = await Favoritos.MarcarAsync(_ana, recetaId);

        Assert.Equal(ResultadoDeFavorito.Correcto, resultado);
        Assert.Single(await Favoritos.ListarMisFavoritasAsync(_ana));
    }

    [Fact]
    public async Task Marcar_UnaPrivadaAjena_NoSePuede()
    {
        var recetaId = await CrearRecetaAsync(_ana);

        var resultado = await Favoritos.MarcarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeFavorito.NoEncontrada, resultado);
        Assert.Empty(_favoritos.Todos);
    }

    [Fact]
    public async Task Marcar_UnaQueNoExiste_NoEncontrada()
    {
        var resultado = await Favoritos.MarcarAsync(_bruno, Guid.NewGuid());

        Assert.Equal(ResultadoDeFavorito.NoEncontrada, resultado);
        Assert.Empty(_favoritos.Todos);
    }

    [Fact]
    public async Task Marcar_DosVeces_NoDuplica()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        await Favoritos.MarcarAsync(_bruno, recetaId);
        var segunda = await Favoritos.MarcarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeFavorito.Correcto, segunda);
        Assert.Single(_favoritos.Todos);
        Assert.Single(await Favoritos.ListarMisFavoritasAsync(_bruno));
    }

    [Fact]
    public async Task Desmarcar_LoQueNoEstabaMarcado_NoEsError()
    {
        // Dos pestañas abiertas producirían este caso sin que nadie se equivoque.
        var recetaId = await CrearPublicaAsync(_ana);

        await Favoritos.DesmarcarAsync(_bruno, recetaId);

        Assert.Empty(_favoritos.Todos);
    }

    [Fact]
    public async Task Desmarcar_QuitaLaMarca()
    {
        var recetaId = await CrearPublicaAsync(_ana);
        await Favoritos.MarcarAsync(_bruno, recetaId);

        await Favoritos.DesmarcarAsync(_bruno, recetaId);

        Assert.Empty(_favoritos.Todos);
        Assert.Empty(await Favoritos.ListarMisFavoritasAsync(_bruno));
    }

    [Fact]
    public async Task Listar_NoDevuelveLoQueDejoDeEstarPublicado()
    {
        // El corazón de la feature. La marca sobrevive al cambio de visibilidad,
        // así que sin este filtro los favoritos serían una forma de seguir viendo
        // lo que su autora dejó de compartir, o lo que se retiró por moderación.
        var recetaId = await CrearPublicaAsync(_ana);
        await Favoritos.MarcarAsync(_bruno, recetaId);

        await RecetasDe.CambiarVisibilidadAsync(_ana, recetaId, publicar: false);

        Assert.Empty(await Favoritos.ListarMisFavoritasAsync(_bruno));

        // Pero la marca sigue ahí: no se pierde por una decisión ajena.
        Assert.Single(_favoritos.Todos);

        await RecetasDe.CambiarVisibilidadAsync(_ana, recetaId, publicar: true);

        Assert.Single(await Favoritos.ListarMisFavoritasAsync(_bruno));
    }

    [Fact]
    public async Task Desmarcar_SigueSiendoPosibleSobreLoQueYaNoSeVe()
    {
        // Si desmarcar exigiera poder verla, quedaría una fila que su dueño no
        // tiene forma de quitar.
        var recetaId = await CrearPublicaAsync(_ana);
        await Favoritos.MarcarAsync(_bruno, recetaId);
        await RecetasDe.CambiarVisibilidadAsync(_ana, recetaId, publicar: false);

        await Favoritos.DesmarcarAsync(_bruno, recetaId);

        Assert.Empty(_favoritos.Todos);
    }

    [Fact]
    public async Task Listar_NoMezclaLosFavoritosDeOtro()
    {
        var deAna = await CrearPublicaAsync(_ana);
        var deBruno = await CrearPublicaAsync(_bruno);

        await Favoritos.MarcarAsync(_ana, deBruno);
        await Favoritos.MarcarAsync(_bruno, deAna);

        Assert.Equal(deBruno, Assert.Single(await Favoritos.ListarMisFavoritasAsync(_ana)).Id);
        Assert.Equal(deAna, Assert.Single(await Favoritos.ListarMisFavoritasAsync(_bruno)).Id);
    }

    [Fact]
    public async Task Marcar_NoTocaLaReceta()
    {
        // Marcar no es un cambio de la receta. Si moviera su fecha de
        // modificación, su autora vería saltar la receta en su recetario cada vez
        // que alguien la marca: un contador de favoritos con otro disfraz.
        var recetaId = await CrearPublicaAsync(_ana);
        var antes = (await _recetas.BuscarPorIdAsync(recetaId))!.FechaDeModificacion;

        _reloj.Avanzar(TimeSpan.FromHours(3));
        await Favoritos.MarcarAsync(_bruno, recetaId);

        Assert.Equal(antes, (await _recetas.BuscarPorIdAsync(recetaId))!.FechaDeModificacion);
    }

    [Fact]
    public async Task EsFavorita_LoDiceParaCadaUsuario()
    {
        var recetaId = await CrearPublicaAsync(_ana);
        await Favoritos.MarcarAsync(_bruno, recetaId);

        Assert.True(await Favoritos.EsFavoritaAsync(_bruno, recetaId));
        Assert.False(await Favoritos.EsFavoritaAsync(_ana, recetaId));
    }

    // ------------------------------------------------------------- Auxiliares

    private async Task<Guid> CrearRecetaAsync(Guid autor)
    {
        var (_, receta) = await RecetasDe.CrearAsync(autor, new DatosDeReceta(
            "Tortilla",
            TipoDePlato.PlatoPrincipal,
            "Pasos",
            [new LineaDeIngrediente("Patata", 500m, Unidad.Gramo)]));

        return receta!.Id;
    }

    private async Task<Guid> CrearPublicaAsync(Guid autor)
    {
        var recetaId = await CrearRecetaAsync(autor);
        await RecetasDe.CambiarVisibilidadAsync(autor, recetaId, publicar: true);

        return recetaId;
    }
}
