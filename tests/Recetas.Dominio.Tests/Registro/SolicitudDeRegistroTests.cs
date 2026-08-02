using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Dominio.Tests.Registro;

public class SolicitudDeRegistroTests
{
    private static readonly DateTimeOffset Momento = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static SolicitudDeRegistro Crear() =>
        SolicitudDeRegistro.Crear(CorreoElectronico.Crear("alguien@ejemplo.com"), "hash-del-token", Momento);

    [Fact]
    public void ReciénCreada_EstaVigente()
    {
        Assert.True(Crear().EstaVigente(Momento));
    }

    [Fact]
    public void JustoAntesDeCaducar_SigueVigente()
    {
        var solicitud = Crear();

        Assert.True(solicitud.EstaVigente(Momento + SolicitudDeRegistro.Vigencia - TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void EnElInstanteDeCaducidad_YaNoEstaVigente()
    {
        var solicitud = Crear();

        // El límite es exclusivo: a la hora exacta ya no vale.
        Assert.False(solicitud.EstaVigente(Momento + SolicitudDeRegistro.Vigencia));
    }

    [Fact]
    public void Consumir_FuncionaUnaVezYSoloUna()
    {
        var solicitud = Crear();

        Assert.True(solicitud.Consumir(Momento));
        Assert.False(solicitud.Consumir(Momento));
        Assert.True(solicitud.EstaConsumida);
    }

    [Fact]
    public void Consumir_FallaSiYaHabiaCaducado()
    {
        var solicitud = Crear();

        Assert.False(solicitud.Consumir(Momento + SolicitudDeRegistro.Vigencia));
        Assert.False(solicitud.EstaConsumida);
    }

    [Fact]
    public void Invalidar_DejaLaSolicitudInservible()
    {
        var solicitud = Crear();

        solicitud.Invalidar(Momento);

        Assert.False(solicitud.EstaVigente(Momento));
        Assert.False(solicitud.Consumir(Momento));
    }

    [Fact]
    public void Invalidar_NoBorraLaMarcaDeConsumo()
    {
        var solicitud = Crear();
        solicitud.Consumir(Momento);

        solicitud.Invalidar(Momento + TimeSpan.FromMinutes(1));

        Assert.True(solicitud.EstaConsumida);
    }
}
