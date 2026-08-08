using Bunit;
using Recetas.Web.Componentes;
using Recetas.Web.Tests.Dobles;

namespace Recetas.Web.Tests.Componentes;

/// <summary>
/// El componente que se interpone antes de lo irreversible.
/// </summary>
public class ConfirmacionTests : ContextoDeWeb
{
    [Fact]
    public void Pulsar_NoEjecutaLaAccion_SoloPregunta()
    {
        var ejecutada = false;

        var componente = RenderComponent<Confirmacion>(parametros => parametros
            .Add(p => p.Texto, "Borrar receta")
            .Add(p => p.Pregunta, "¿Seguro?")
            .Add(p => p.AlConfirmar, () => ejecutada = true));

        componente.Find("button").Click();

        Assert.False(ejecutada);
        Assert.Contains("¿Seguro?", componente.Markup);
    }

    [Fact]
    public void Confirmar_EjecutaLaAccion()
    {
        var ejecutada = false;

        var componente = RenderComponent<Confirmacion>(parametros => parametros
            .Add(p => p.Texto, "Borrar receta")
            .Add(p => p.Pregunta, "¿Seguro?")
            .Add(p => p.AlConfirmar, () => ejecutada = true));

        componente.Find("button").Click();
        BotonQueDice(componente, "Sí,").Click();

        Assert.True(ejecutada);
    }

    [Fact]
    public void Cancelar_NoEjecutaLaAccion_NiDejaLaPreguntaAbierta()
    {
        var ejecutada = false;

        var componente = RenderComponent<Confirmacion>(parametros => parametros
            .Add(p => p.Texto, "Borrar receta")
            .Add(p => p.Pregunta, "¿Seguro?")
            .Add(p => p.AlConfirmar, () => ejecutada = true));

        componente.Find("button").Click();
        BotonQueDice(componente, "Cancelar").Click();

        Assert.False(ejecutada);
        Assert.DoesNotContain("¿Seguro?", componente.Markup);
    }

    private static AngleSharp.Dom.IElement BotonQueDice(IRenderedFragment componente, string texto) =>
        componente.FindAll("button").First(boton => boton.TextContent.Contains(texto));
}
