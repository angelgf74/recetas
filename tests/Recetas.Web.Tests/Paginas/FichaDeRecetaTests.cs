using Bunit;
using Recetas.Contratos.Recetas;
using Recetas.Web.Pages;
using Recetas.Web.Tests.Dobles;

namespace Recetas.Web.Tests.Paginas;

/// <summary>
/// Qué acciones ofrece la ficha según de quién sea la receta y quién mire.
/// </summary>
/// <remarks>
/// Esta clase existe por un fallo concreto. En la feature 015 se implementó la
/// retirada por moderación con todos sus tests en verde <b>y sin ningún botón que
/// la invocara</b>: los tests comprobaban que el endpoint autorizaba bien, no que
/// hubiera manera de llegar a él.
/// <para>
/// Por eso se afirma sobre <b>texto visible</b> y no sobre clases CSS: lo que
/// importa es si el usuario tiene forma de hacer algo.
/// </para>
/// </remarks>
public class FichaDeRecetaTests : ContextoDeWeb
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void RecetaPropia_OfreceLasAccionesDeAutor_YNoOfreceDenunciar()
    {
        var pantalla = Pintar(Receta(esMia: true));

        Assert.Contains("Editar receta", pantalla.Markup);
        Assert.Contains("Compartir receta", pantalla.Markup);
        Assert.Contains("Borrar receta", pantalla.Markup);

        Assert.DoesNotContain("Denunciar esta receta", pantalla.Markup);
    }

    [Fact]
    public void RecetaAjena_OfreceDenunciar_YNoOfreceLasAccionesDeAutor()
    {
        var pantalla = Pintar(Receta(esMia: false));

        Assert.Contains("Denunciar esta receta", pantalla.Markup);

        Assert.DoesNotContain("Editar receta", pantalla.Markup);
        Assert.DoesNotContain("Compartir receta", pantalla.Markup);
        Assert.DoesNotContain("Borrar receta", pantalla.Markup);
    }

    [Fact]
    public void RecetaAjena_ConPermisoDeRetirada_OfreceRetirarla()
    {
        // ESTE es el test que faltaba en la 015. Sin él, el endpoint de retirada
        // pasó a producción sin que hubiera forma de invocarlo desde la interfaz.
        var pantalla = Pintar(Receta(esMia: false, puedoRetirarla: true));

        Assert.Contains("Retirar de la parte pública", pantalla.Markup);
    }

    [Fact]
    public void RecetaAjena_SinPermisoDeRetirada_NoLaOfrece()
    {
        var pantalla = Pintar(Receta(esMia: false, puedoRetirarla: false));

        Assert.DoesNotContain("Retirar de la parte pública", pantalla.Markup);
    }

    [Fact]
    public void RecetaPropia_NuncaOfreceRetirarla()
    {
        // Sobre lo propio ya están las acciones de autor: ofrecer además la
        // retirada por moderación sería confundir dos cosas distintas.
        var pantalla = Pintar(Receta(esMia: true, puedoRetirarla: true));

        Assert.DoesNotContain("Retirar de la parte pública", pantalla.Markup);
    }

    [Fact]
    public void RecetaAjena_TrasDenunciarla_DejaDeOfrecerlo()
    {
        Respuestas.Para($"recetas/{Id}/denuncias", System.Net.HttpStatusCode.NoContent);

        var pantalla = Pintar(Receta(esMia: false));

        pantalla.FindAll("button").First(b => b.TextContent.Contains("Denunciar esta receta")).Click();
        pantalla.FindAll("button").First(b => b.TextContent.Contains("Enviar denuncia")).Click();

        pantalla.WaitForAssertion(() =>
            Assert.Contains("Hemos recibido tu aviso", pantalla.Markup));

        Assert.DoesNotContain("Denunciar esta receta", pantalla.Markup);
    }

    // ------------------------------------------------------------- Auxiliares

    private IRenderedComponent<FichaDeReceta> Pintar(RespuestaDeReceta receta)
    {
        Respuestas.Para($"recetas/{receta.Id}", receta);

        var pantalla = RenderComponent<FichaDeReceta>(parametros => parametros
            .Add(p => p.Id, receta.Id));

        // La ficha carga la receta al iniciarse; sin esperar, se afirmaría sobre
        // el "Cargando…".
        pantalla.WaitForAssertion(() => Assert.Contains(receta.Nombre, pantalla.Markup));

        return pantalla;
    }

    private static RespuestaDeReceta Receta(bool esMia, bool puedoRetirarla = false) =>
        new(
            Id,
            "Tortilla de patatas",
            "PlatoPrincipal",
            "Batir, freír, cuajar.",
            esMia || !puedoRetirarla ? "Privada" : "Publica",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new LineaDeIngredienteRespuesta("Patata", 500m, "Gramo")],
            [],
            esMia,
            Raciones: null,
            RacionesMostradas: null,
            PuedoRetirarla: puedoRetirarla);
}
