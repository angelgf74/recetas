using Bunit;
using Recetas.Web.Layout;
using Recetas.Web.Tests.Dobles;

namespace Recetas.Web.Tests.Layout;

/// <summary>
/// El menú de la cabecera se pliega detrás de un botón en pantallas estrechas.
/// </summary>
/// <remarks>
/// bUnit no ejecuta un navegador de verdad, así que no ve si el CSS oculta el
/// botón o la navegación a partir de 40rem: eso hay que mirarlo en pantalla.
/// Lo que sí prueba es la parte que depende de Blazor y no del CSS: que el
/// botón empiece cerrado, que abra y cierre, y que un enlace lo cierre.
/// </remarks>
public class MainLayoutTests : ContextoDeWeb
{
    [Fact]
    public void ElMenu_EmpiezaCerrado()
    {
        var pantalla = Pintar();

        var boton = pantalla.Find("button.boton-menu");
        Assert.Equal("false", boton.GetAttribute("aria-expanded"));
        Assert.DoesNotContain("navegacion-abierta", pantalla.Find("nav").ClassList);
    }

    [Fact]
    public void AlPulsarElBoton_SeAbre()
    {
        var pantalla = Pintar();

        pantalla.Find("button.boton-menu").Click();

        Assert.Equal("true", pantalla.Find("button.boton-menu").GetAttribute("aria-expanded"));
        Assert.Contains("navegacion-abierta", pantalla.Find("nav").ClassList);
    }

    [Fact]
    public void AlPulsarloDeNuevo_SeCierra()
    {
        var pantalla = Pintar();
        var boton = pantalla.Find("button.boton-menu");

        boton.Click();
        boton.Click();

        Assert.Equal("false", boton.GetAttribute("aria-expanded"));
        Assert.DoesNotContain("navegacion-abierta", pantalla.Find("nav").ClassList);
    }

    [Fact]
    public void AlPulsarUnEnlace_SeCierra()
    {
        // Sin esto, en móvil el panel se quedaría desplegado tapando la
        // pantalla siguiente hasta que el usuario lo cerrara a mano.
        var pantalla = Pintar();
        pantalla.Find("button.boton-menu").Click();

        pantalla.FindAll("nav a").First(enlace => enlace.TextContent == "Favoritos").Click();

        Assert.Equal("false", pantalla.Find("button.boton-menu").GetAttribute("aria-expanded"));
    }

    private IRenderedComponent<MainLayout> Pintar() => RenderComponent<MainLayout>();
}
