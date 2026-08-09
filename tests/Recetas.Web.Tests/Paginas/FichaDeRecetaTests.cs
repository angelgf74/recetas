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

    [Fact]
    public void SinMarcar_OfreceGuardarEnFavoritos()
    {
        var pantalla = Pintar(Receta(esMia: false, esFavorita: false));

        Assert.Contains("Guardar en favoritos", pantalla.Markup);
        Assert.DoesNotContain("Quitar de favoritos", pantalla.Markup);
    }

    [Fact]
    public void Marcada_OfreceQuitarla()
    {
        var pantalla = Pintar(Receta(esMia: false, esFavorita: true));

        Assert.Contains("Quitar de favoritos", pantalla.Markup);
        Assert.DoesNotContain("Guardar en favoritos", pantalla.Markup);
    }

    [Fact]
    public void RecetaPropia_TambienSePuedeGuardar()
    {
        // La regla es "lo que puedas ver". Si el botón desapareciera sobre lo
        // propio, la interfaz tendría una excepción que la API no tiene.
        var pantalla = Pintar(Receta(esMia: true));

        Assert.Contains("Guardar en favoritos", pantalla.Markup);
    }

    [Fact]
    public void ConFotos_LaFichaPideMiniaturasYNoLosArchivosCompletos()
    {
        // Es la razón de ser de la 022. Antes la ficha descargaba cada archivo
        // entero —y en base64, un 33 % más— solo para enseñar la receta.
        var foto = new FotoRespuesta(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "Jpeg", 3_000_000);
        Respuestas.Para($"recetas/{Id}/fotos/{foto.Id}/miniatura", System.Net.HttpStatusCode.OK);

        var pantalla = Pintar(Receta(esMia: true, fotos: [foto]));

        pantalla.WaitForAssertion(() =>
            Assert.Contains($"recetas/{Id}/fotos/{foto.Id}/miniatura", Respuestas.Peticiones));

        Assert.DoesNotContain($"recetas/{Id}/fotos/{foto.Id}", Respuestas.Peticiones);
    }

    [Fact]
    public void ConFotos_OfreceAbrirlasATamanoCompleto()
    {
        var foto = new FotoRespuesta(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "Jpeg", 3_000_000);
        Respuestas.Para($"recetas/{Id}/fotos/{foto.Id}/miniatura", System.Net.HttpStatusCode.OK);
        Respuestas.Para($"recetas/{Id}/fotos/{foto.Id}", System.Net.HttpStatusCode.OK);

        var pantalla = Pintar(Receta(esMia: true, fotos: [foto]));

        // Sin este botón, pasar la ficha a miniaturas dejaría la foto completa
        // inalcanzable: el endpoint exige cabecera y no se puede enlazar.
        pantalla.Find("button[aria-label='Ver la foto 1 a tamaño completo']").Click();

        pantalla.WaitForAssertion(() => Assert.Contains("Foto 1 de 1", pantalla.Markup));
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

    private static RespuestaDeReceta Receta(
        bool esMia,
        bool puedoRetirarla = false,
        bool esFavorita = false,
        IReadOnlyCollection<FotoRespuesta>? fotos = null) =>
        new(
            Id,
            "Tortilla de patatas",
            "PlatoPrincipal",
            "Batir, freír, cuajar.",
            esMia || !puedoRetirarla ? "Privada" : "Publica",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new LineaDeIngredienteRespuesta("Patata", 500m, "Gramo")],
            fotos ?? [],
            esMia,
            Raciones: null,
            RacionesMostradas: null,
            PuedoRetirarla: puedoRetirarla,
            EsFavorita: esFavorita);
}
