using Recetas.Dominio.Puertos;
using Recetas.Dominio.Salud;

namespace Recetas.Aplicacion.Salud;

/// <summary>
/// Caso de uso que informa de si el sistema puede operar.
/// Establece el patrón que siguen los casos de uso reales: depende de puertos,
/// nunca de tipos de infraestructura.
/// </summary>
public sealed class ConsultarSalud(
    IComprobadorDeAlmacen almacen,
    IComprobadorDeAlmacenDeFotos almacenDeFotos)
{
    public async Task<EstadoDeSalud> EjecutarAsync(CancellationToken cancelacion = default)
    {
        // En paralelo porque son independientes. Encadenadas, una sonda con la
        // base lenta y el disco lento tardaría la suma de las dos, y las sondas
        // tienen tiempo de espera.
        var baseDeDatos = ComprobarAsync(() => almacen.RespondeAsync(cancelacion));
        var almacenamiento = ComprobarAsync(() => almacenDeFotos.AceptaFotosAsync(cancelacion));

        await Task.WhenAll(baseDeDatos, almacenamiento);

        return new EstadoDeSalud(baseDeDatos.Result, almacenamiento.Result);
    }

    /// <summary>
    /// Una dependencia que revienta al comprobarla es una dependencia que no
    /// está sana: se traduce a <c>false</c>, nunca se propaga. Si la excepción
    /// llegara al endpoint, la sonda recibiría un <c>500</c> y no sabría si el
    /// problema es la dependencia o el propio diagnóstico.
    /// </summary>
    private static async Task<bool> ComprobarAsync(Func<Task<bool>> comprobacion)
    {
        try
        {
            return await comprobacion();
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            return false;
        }
    }
}
