using Recetas.Dominio.Puertos;
using Recetas.Dominio.Salud;

namespace Recetas.Aplicacion.Salud;

/// <summary>
/// Caso de uso que informa de si el sistema puede operar.
/// Establece el patrón que seguirán los casos de uso reales: depende de puertos,
/// nunca de tipos de infraestructura.
/// </summary>
public sealed class ConsultarSalud(IComprobadorDeAlmacen almacen)
{
    public async Task<EstadoDeSalud> EjecutarAsync(CancellationToken cancelacion = default)
    {
        var almacenResponde = await almacen.RespondeAsync(cancelacion);

        return almacenResponde ? EstadoDeSalud.Correcto : EstadoDeSalud.Degradado;
    }
}
