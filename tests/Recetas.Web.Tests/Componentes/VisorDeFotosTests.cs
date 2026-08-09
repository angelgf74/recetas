using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Recetas.Contratos.Recetas;
using Recetas.Web.Componentes;
using Recetas.Web.Tests.Dobles;

namespace Recetas.Web.Tests.Componentes;

/// <summary>
/// Qué ofrece el visor según cuántas fotos haya y quién mire.
/// </summary>
/// <remarks>
/// bUnit pinta el componente y dispara eventos, así que responde a "¿hay botón?"
/// y "¿cambia de foto?". <b>No dice si la superposición se ve bien</b> ni si
/// Escape llega en un navegador de verdad: eso hay que mirarlo en pantalla.
/// </remarks>
public class VisorDeFotosTests : ContextoDeWeb
{
    private static readonly Guid RecetaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly FotoRespuesta[] TresFotos =
    [
        new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "Jpeg", 1000),
        new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), "Jpeg", 2000),
        new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), "Jpeg", 3000)
    ];

    [Fact]
    public void SeAbrePorLaFotoQueSeLePide()
    {
        var visor = Pintar(TresFotos, indice: 1);

        Assert.Contains("Foto 2 de 3", visor.Markup);

        // La que se pide a la API es la segunda, no la primera.
        Assert.Contains($"recetas/{RecetaId}/fotos/{TresFotos[1].Id}", Respuestas.Peticiones);
    }

    [Fact]
    public void Siguiente_PasaDeFoto()
    {
        var visor = Pintar(TresFotos);

        Assert.Contains("Foto 1 de 3", visor.Markup);

        Pulsar(visor, "Foto siguiente");

        Assert.Contains("Foto 2 de 3", visor.Markup);
    }

    [Fact]
    public void Anterior_DesdeLaPrimera_VaALaUltima()
    {
        // Dar la vuelta en lugar de dejar un botón muerto: con tres fotos, un
        // control que no hace nada se lee como que la aplicación se ha colgado.
        var visor = Pintar(TresFotos);

        Pulsar(visor, "Foto anterior");

        Assert.Contains("Foto 3 de 3", visor.Markup);
    }

    [Fact]
    public void ConUnaSolaFoto_NoOfreceNavegacion()
    {
        var visor = Pintar([TresFotos[0]]);

        Assert.DoesNotContain("Foto siguiente", visor.Markup);
        Assert.DoesNotContain("Foto anterior", visor.Markup);
        Assert.Contains("Foto 1 de 1", visor.Markup);
    }

    [Fact]
    public void Cerrar_AvisaAQuienLoAbrio()
    {
        var cerrado = false;
        var visor = Pintar(TresFotos, alCerrar: () => cerrado = true);

        Pulsar(visor, "Cerrar");

        Assert.True(cerrado);
    }

    [Fact]
    public void SinPermiso_NoOfreceBorrar()
    {
        var visor = Pintar(TresFotos, puedeBorrar: false);

        Assert.DoesNotContain("Borrar foto", visor.Markup);
    }

    [Fact]
    public void ConPermiso_BorraLaFotoQueSeEstaViendo_YCierra()
    {
        Guid? borrada = null;
        var cerrado = false;

        var visor = Pintar(
            TresFotos,
            indice: 2,
            puedeBorrar: true,
            alCerrar: () => cerrado = true,
            alBorrar: id => borrada = id);

        // La confirmación es de dos pasos, como en el resto del producto.
        Pulsar(visor, "Borrar foto");
        visor.FindAll("button").First(b => b.TextContent.Contains("Sí, borrar foto")).Click();

        Assert.Equal(TresFotos[2].Id, borrada);
        Assert.True(cerrado);
    }

    // ------------------------------------------------------------- Auxiliares

    private static void Pulsar(IRenderedComponent<VisorDeFotos> visor, string etiqueta) =>
        visor.Find($"button[aria-label='{etiqueta}']").Click();

    private IRenderedComponent<VisorDeFotos> Pintar(
        IReadOnlyList<FotoRespuesta> fotos,
        int indice = 0,
        bool puedeBorrar = false,
        Action? alCerrar = null,
        Action<Guid>? alBorrar = null)
    {
        // Sin cuerpo: al componente le basta con que la descarga no falle, y una
        // imagen de verdad no añadiría nada a lo que se prueba aquí.
        foreach (var foto in fotos)
        {
            Respuestas.Para($"recetas/{RecetaId}/fotos/{foto.Id}", HttpStatusCode.OK);
        }

        return RenderComponent<VisorDeFotos>(parametros => parametros
            .Add(p => p.RecetaId, RecetaId)
            .Add(p => p.Fotos, fotos)
            .Add(p => p.Indice, indice)
            .Add(p => p.PuedeBorrar, puedeBorrar)
            .Add(p => p.AlCerrar, EventCallback.Factory.Create(this, () => alCerrar?.Invoke()))
            .Add(p => p.AlBorrar, EventCallback.Factory.Create<Guid>(this, id => alBorrar?.Invoke(id))));
    }
}
