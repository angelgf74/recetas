using Recetas.Aplicacion.Salud;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Salud;

namespace Recetas.Aplicacion.Tests.Salud;

/// <summary>
/// Ejemplo del patrón de test unitario del proyecto: el caso de uso se prueba
/// contra dobles de los puertos, sin base de datos ni Docker de por medio.
/// </summary>
public class ConsultarSaludTests
{
    [Fact]
    public async Task Estado_EsCorrecto_CuandoElAlmacenResponde()
    {
        var casoDeUso = new ConsultarSalud(new ComprobadorFalso(responde: true));

        var estado = await casoDeUso.EjecutarAsync();

        Assert.Equal(EstadoDeSalud.Correcto, estado);
    }

    [Fact]
    public async Task Estado_EsDegradado_CuandoElAlmacenNoResponde()
    {
        var casoDeUso = new ConsultarSalud(new ComprobadorFalso(responde: false));

        var estado = await casoDeUso.EjecutarAsync();

        Assert.Equal(EstadoDeSalud.Degradado, estado);
    }

    private sealed class ComprobadorFalso(bool responde) : IComprobadorDeAlmacen
    {
        public Task<bool> RespondeAsync(CancellationToken cancelacion = default) =>
            Task.FromResult(responde);
    }
}
