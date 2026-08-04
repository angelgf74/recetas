using Recetas.Dominio.Usuarios;

namespace Recetas.Dominio.Tests.Usuarios;

public class UsuarioTests
{
    private static readonly DateTimeOffset Alta = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Usuario Crear() =>
        Usuario.Crear(CorreoElectronico.Crear("alguien@ejemplo.com"), "hash-original", Alta);

    [Fact]
    public void ReciénCreado_NoTieneFechaDeCambioDeContrasena()
    {
        Assert.Null(Crear().FechaDeCambioDeContrasena);
    }

    [Fact]
    public void CambiarContrasena_SustituyeElHashYAnotaCuando()
    {
        var usuario = Crear();
        var cambio = Alta.AddDays(30);

        usuario.CambiarContrasena("hash-nuevo", cambio);

        Assert.Equal("hash-nuevo", usuario.HashDeContrasena);
        Assert.Equal(cambio, usuario.FechaDeCambioDeContrasena);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CambiarContrasena_RechazaUnHashVacio(string hash)
    {
        var usuario = Crear();

        Assert.Throws<ArgumentException>(() => usuario.CambiarContrasena(hash, Alta));

        // Y el usuario se queda como estaba: un hash en blanco dejaría la cuenta
        // sin contraseña utilizable.
        Assert.Equal("hash-original", usuario.HashDeContrasena);
    }

    [Fact]
    public void CambiarContrasena_NoTocaElCorreoNiLaFechaDeAlta()
    {
        var usuario = Crear();

        usuario.CambiarContrasena("hash-nuevo", Alta.AddDays(1));

        Assert.Equal("alguien@ejemplo.com", usuario.Correo.Valor);
        Assert.Equal(Alta, usuario.FechaDeAlta);
    }
}
