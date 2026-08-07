using Recetas.Dominio.Puertos;

namespace Recetas.Aplicacion.Cuentas;

/// <param name="Recetas">Cuántas recetas tiene, públicas y privadas.</param>
/// <param name="Fotos">Cuántas fotos hay repartidas entre ellas.</param>
public sealed record ResumenDeLaCuenta(int Recetas, int Fotos);

/// <summary>
/// Qué hay dentro de una cuenta.
/// </summary>
/// <remarks>
/// Existe para que la pantalla de baja pueda decir <b>qué se va a perder</b> antes
/// de que el usuario confirme. "Se borrarán 23 recetas y 41 fotos" frena a quien
/// se ha equivocado de botón; "esta acción es irreversible" no frena a nadie
/// porque se lee en todas partes.
/// </remarks>
public sealed class ConsultarResumenDeLaCuenta(IRepositorioDeRecetas recetas)
{
    public async Task<ResumenDeLaCuenta> EjecutarAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default)
    {
        var suyas = await recetas.ListarPorAutorAsync(usuarioId, cancelacion);

        return new ResumenDeLaCuenta(suyas.Count, suyas.Sum(receta => receta.Fotos.Count));
    }
}
