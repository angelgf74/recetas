using Recetas.Aplicacion.Salud;
using Recetas.Dominio.Puertos;

namespace Recetas.Aplicacion.Tests.Salud;

/// <summary>
/// El caso de uso se prueba contra dobles de los puertos, sin base de datos ni
/// Docker de por medio.
/// </summary>
public class ConsultarSaludTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public async Task Estado_EsCorrecto_SoloSiTodasLasPiezasResponden(
        bool baseDeDatos,
        bool almacenamiento,
        bool esperado)
    {
        var casoDeUso = new ConsultarSalud(
            new ComprobadorFalso(baseDeDatos),
            new ComprobadorDeFotosFalso(almacenamiento));

        var estado = await casoDeUso.EjecutarAsync();

        Assert.Equal(esperado, estado.EsCorrecto);
        Assert.Equal(baseDeDatos, estado.BaseDeDatos);
        Assert.Equal(almacenamiento, estado.Almacenamiento);
    }

    [Fact]
    public async Task Estado_DiceQuePiezaFalla_NoSoloQueAlgoFalla()
    {
        // El motivo de que EstadoDeSalud lleve detalle: quien recibe el aviso
        // tiene que saber dónde mirar sin entrar en el servidor.
        var casoDeUso = new ConsultarSalud(
            new ComprobadorFalso(responde: true),
            new ComprobadorDeFotosFalso(acepta: false));

        var estado = await casoDeUso.EjecutarAsync();

        Assert.False(estado.EsCorrecto);
        Assert.True(estado.BaseDeDatos);
        Assert.False(estado.Almacenamiento);
    }

    [Fact]
    public async Task Estado_EsDegradado_SiComprobarLaBaseRevienta()
    {
        // Si la excepción llegara al endpoint, la sonda recibiría un 500 y no
        // sabría si el problema es la dependencia o el propio diagnóstico.
        var casoDeUso = new ConsultarSalud(
            new ComprobadorQueRevienta(),
            new ComprobadorDeFotosFalso(acepta: true));

        var estado = await casoDeUso.EjecutarAsync();

        Assert.False(estado.EsCorrecto);
        Assert.False(estado.BaseDeDatos);
        Assert.True(estado.Almacenamiento);
    }

    [Fact]
    public async Task Estado_EsDegradado_SiComprobarElAlmacenamientoRevienta()
    {
        var casoDeUso = new ConsultarSalud(
            new ComprobadorFalso(responde: true),
            new ComprobadorDeFotosQueRevienta());

        var estado = await casoDeUso.EjecutarAsync();

        Assert.False(estado.EsCorrecto);
        Assert.True(estado.BaseDeDatos);
        Assert.False(estado.Almacenamiento);
    }

    [Fact]
    public async Task Comprobaciones_SeLanzanEnParalelo()
    {
        // Encadenadas, una sonda con la base lenta y el disco lento tardaría la
        // suma de las dos, y las sondas tienen tiempo de espera. Cada doble
        // tarda 200 ms: en serie serían 400 y aquí se exige bastante menos.
        var casoDeUso = new ConsultarSalud(
            new ComprobadorLento(TimeSpan.FromMilliseconds(200)),
            new ComprobadorDeFotosLento(TimeSpan.FromMilliseconds(200)));

        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        await casoDeUso.EjecutarAsync();
        cronometro.Stop();

        Assert.True(
            cronometro.ElapsedMilliseconds < 350,
            $"Las comprobaciones parecen encadenadas: tardaron {cronometro.ElapsedMilliseconds} ms.");
    }

    // ------------------------------------------------------------- Auxiliares

    private sealed class ComprobadorFalso(bool responde) : IComprobadorDeAlmacen
    {
        public Task<bool> RespondeAsync(CancellationToken cancelacion = default) =>
            Task.FromResult(responde);
    }

    private sealed class ComprobadorDeFotosFalso(bool acepta) : IComprobadorDeAlmacenDeFotos
    {
        public Task<bool> AceptaFotosAsync(CancellationToken cancelacion = default) =>
            Task.FromResult(acepta);
    }

    private sealed class ComprobadorQueRevienta : IComprobadorDeAlmacen
    {
        public Task<bool> RespondeAsync(CancellationToken cancelacion = default) =>
            throw new InvalidOperationException("Fallo simulado al comprobar la base.");
    }

    private sealed class ComprobadorDeFotosQueRevienta : IComprobadorDeAlmacenDeFotos
    {
        public Task<bool> AceptaFotosAsync(CancellationToken cancelacion = default) =>
            throw new IOException("Fallo simulado al comprobar el disco.");
    }

    private sealed class ComprobadorLento(TimeSpan espera) : IComprobadorDeAlmacen
    {
        public async Task<bool> RespondeAsync(CancellationToken cancelacion = default)
        {
            await Task.Delay(espera, cancelacion);
            return true;
        }
    }

    private sealed class ComprobadorDeFotosLento(TimeSpan espera) : IComprobadorDeAlmacenDeFotos
    {
        public async Task<bool> AceptaFotosAsync(CancellationToken cancelacion = default)
        {
            await Task.Delay(espera, cancelacion);
            return true;
        }
    }
}
