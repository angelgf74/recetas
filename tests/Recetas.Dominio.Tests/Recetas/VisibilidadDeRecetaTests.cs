using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Tests.Recetas;

/// <summary>
/// La distinción entre "puede verla" y "es suya" es el corazón de la feature 005.
/// Confundirlas permitiría a cualquiera editar las recetas públicas de los demás.
/// </summary>
public class VisibilidadDeRecetaTests
{
    private static readonly DateTimeOffset Momento = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Autora = Guid.NewGuid();
    private static readonly Guid Otro = Guid.NewGuid();

    private static Receta Crear() =>
        Receta.Crear(Autora, "Tortilla", TipoDePlato.PlatoPrincipal, "Pasos", Momento);

    [Fact]
    public void Privada_SoloLaVeSuAutora()
    {
        var receta = Crear();

        Assert.True(receta.PuedeVerla(Autora));
        Assert.False(receta.PuedeVerla(Otro));
    }

    [Fact]
    public void Publicada_LaVeCualquierUsuario()
    {
        var receta = Crear();
        receta.Publicar(Momento);

        Assert.True(receta.PuedeVerla(Otro));
    }

    [Fact]
    public void Publicada_SigueSiendoSoloDeSuAutora()
    {
        var receta = Crear();
        receta.Publicar(Momento);

        // Lo que impide que un tercero la edite o la borre: verla no es tenerla.
        Assert.True(receta.EsDe(Autora));
        Assert.False(receta.EsDe(Otro));
    }

    [Fact]
    public void Despublicar_LaVuelveAOcultar()
    {
        var receta = Crear();
        receta.Publicar(Momento);
        receta.Despublicar(Momento.AddHours(1));

        Assert.Equal(Visibilidad.Privada, receta.Visibilidad);
        Assert.False(receta.PuedeVerla(Otro));
    }

    [Fact]
    public void Publicar_MueveLaFechaDeModificacion()
    {
        var receta = Crear();

        receta.Publicar(Momento.AddHours(2));

        Assert.Equal(Momento.AddHours(2), receta.FechaDeModificacion);
    }

    [Fact]
    public void Publicar_EsIdempotente()
    {
        var receta = Crear();
        receta.Publicar(Momento);
        var tras = receta.FechaDeModificacion;

        // Publicar algo ya público no es un error ni vuelve a marcar modificación:
        // el usuario pide un estado, no una transición.
        receta.Publicar(Momento.AddHours(5));

        Assert.Equal(Visibilidad.Publica, receta.Visibilidad);
        Assert.Equal(tras, receta.FechaDeModificacion);
    }

    [Fact]
    public void Despublicar_EsIdempotente()
    {
        var receta = Crear();
        var inicial = receta.FechaDeModificacion;

        receta.Despublicar(Momento.AddHours(5));

        Assert.Equal(Visibilidad.Privada, receta.Visibilidad);
        Assert.Equal(inicial, receta.FechaDeModificacion);
    }

    [Fact]
    public void Actualizar_NoCambiaLaVisibilidadDeUnaPublicada()
    {
        var receta = Crear();
        receta.Publicar(Momento);

        receta.Actualizar("Otro nombre", TipoDePlato.Postre, "Otros pasos", Momento.AddHours(1));

        // Editar no puede despublicar por accidente.
        Assert.Equal(Visibilidad.Publica, receta.Visibilidad);
    }
}
