using Microsoft.Extensions.Logging.Abstractions;
using Recetas.Aplicacion.Moderacion;
using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Tests.Dobles;
using Recetas.Dominio.Recetas;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Tests.Moderacion;

/// <summary>
/// Retirada por moderación: quién puede, qué se puede retirar y a quién se avisa.
/// </summary>
public class RetirarPorModeracionTests
{
    private const string CorreoDeAna = "ana@ejemplo.com";

    private readonly RepositorioDeRecetasEnMemoria _recetas = new();
    private readonly RepositorioDeIngredientesEnMemoria _ingredientes = new();
    private readonly RepositorioDeUsuariosEnMemoria _usuarios = new();
    private readonly AlmacenDeFotosEnMemoria _almacen = new();
    private readonly EnviadorDeCorreoEspia _correo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

    private Guid _ana;
    private Guid _bruno;

    private RetirarPorModeracion Retirada =>
        new(_recetas, _usuarios, _correo, _reloj, NullLogger<RetirarPorModeracion>.Instance);

    private GestionDeRecetas RecetasDe =>
        new(_recetas, new ResolverIngredientes(_ingredientes), new ResolverEtiquetas(new RepositorioDeEtiquetasEnMemoria()), _almacen, _reloj);

    public RetirarPorModeracionTests()
    {
        _ana = CrearUsuario(CorreoDeAna);
        _bruno = CrearUsuario("bruno@ejemplo.com");
    }

    [Fact]
    public async Task Retirar_UnaPublicaAjena_LaDespublicaYAvisaAlAutor()
    {
        var recetaId = await CrearPublicaAsync(_ana);

        var resultado = await Retirada.EjecutarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeRetirada.Correcto, resultado);

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.False(receta!.EsPublica);

        // Retirar no borra: su autora la conserva.
        Assert.Equal(_ana, receta.AutorId);

        var (destinatario, nombre) = Assert.Single(_correo.AvisosDeRetirada);
        Assert.Equal(CorreoDeAna, destinatario);
        Assert.Equal("Tortilla", nombre);
    }

    [Fact]
    public async Task Retirar_UnaPrivadaAjena_NoHaceNada()
    {
        // Nadie ha podido verla para denunciarla, así que no hay nada que retirar.
        var recetaId = await CrearRecetaAsync(_ana);

        var resultado = await Retirada.EjecutarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeRetirada.NoEncontrada, resultado);
        Assert.Empty(_correo.AvisosDeRetirada);
    }

    [Fact]
    public async Task Retirar_UnaPropia_NoPasaPorAqui()
    {
        // El responsable despublicando lo suyo es el autor despublicando: por esta
        // vía se autoenviaría un aviso de retirada.
        var recetaId = await CrearPublicaAsync(_bruno);

        var resultado = await Retirada.EjecutarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeRetirada.NoEncontrada, resultado);
        Assert.Empty(_correo.AvisosDeRetirada);

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.True(receta!.EsPublica);
    }

    [Fact]
    public async Task Retirar_UnaQueNoExiste_NoEncontrada()
    {
        var resultado = await Retirada.EjecutarAsync(_bruno, Guid.NewGuid());

        Assert.Equal(ResultadoDeRetirada.NoEncontrada, resultado);
        Assert.Empty(_correo.AvisosDeRetirada);
    }

    [Fact]
    public async Task Retirar_SiElCorreoFalla_LaRecetaSigueRetirada()
    {
        var recetaId = await CrearPublicaAsync(_ana);
        _correo.FallaAlEnviar = true;

        var resultado = await Retirada.EjecutarAsync(_bruno, recetaId);

        // El aviso es un extra. Devolver error haría pensar al responsable que la
        // receta sigue pública, y volvería a intentarlo sobre algo ya retirado.
        Assert.Equal(ResultadoDeRetirada.Correcto, resultado);

        var receta = await _recetas.BuscarPorIdAsync(recetaId);
        Assert.False(receta!.EsPublica);
    }

    [Fact]
    public async Task Retirar_DosVeces_LaSegundaNoAvisa()
    {
        // Sin esto, reintentar la retirada mandaría al autor un aviso por cada
        // clic sobre una receta que ya estaba fuera.
        var recetaId = await CrearPublicaAsync(_ana);

        await Retirada.EjecutarAsync(_bruno, recetaId);
        var segunda = await Retirada.EjecutarAsync(_bruno, recetaId);

        Assert.Equal(ResultadoDeRetirada.NoEncontrada, segunda);
        Assert.Single(_correo.AvisosDeRetirada);
    }

    // ------------------------------------------------------------- Auxiliares

    private Guid CrearUsuario(string correo)
    {
        var usuario = Usuario.Crear(CorreoElectronico.Crear(correo), "hash", _reloj.Ahora);
        _usuarios.AnadirAsync(usuario).GetAwaiter().GetResult();

        return usuario.Id;
    }

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
