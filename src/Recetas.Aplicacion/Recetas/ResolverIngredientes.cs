using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <param name="Nombre">Nombre tal como lo escribió el usuario, sin normalizar.</param>
/// <param name="Cantidad">Cantidad, o <c>null</c> si la unidad es <see cref="Unidad.AlGusto"/>.</param>
/// <param name="Unidad">Unidad de medida.</param>
public sealed record LineaDeIngrediente(string Nombre, decimal? Cantidad, Unidad Unidad);

/// <summary>
/// Traduce nombres de ingrediente escritos por el usuario a entidades del
/// catálogo, creando las que aún no existan.
/// </summary>
/// <remarks>
/// Es el único punto donde se dan de alta ingredientes, y por eso es el único
/// sitio donde puede aparecer un duplicado. Concentrarlo aquí es lo que mantiene
/// el catálogo limpio para la búsqueda de la feature 006.
/// </remarks>
public sealed class ResolverIngredientes(IRepositorioDeIngredientes ingredientes)
{
    public async Task<IReadOnlyList<(Guid IngredienteId, decimal? Cantidad, Unidad Unidad)>?> EjecutarAsync(
        IReadOnlyCollection<LineaDeIngrediente> lineas,
        CancellationToken cancelacion = default)
    {
        if (lineas.Count == 0)
        {
            return null;
        }

        var normalizadas = new List<(NombreDeIngrediente Nombre, decimal? Cantidad, Unidad Unidad)>(lineas.Count);

        foreach (var linea in lineas)
        {
            if (!NombreDeIngrediente.TryCrear(linea.Nombre, out var nombre))
            {
                return null;
            }

            if (linea.Unidad != Unidad.AlGusto && linea.Cantidad is null or <= 0)
            {
                return null;
            }

            normalizadas.Add((nombre, linea.Cantidad, linea.Unidad));
        }

        // Se comprueba tras normalizar, no antes: "Tomate" y " tomate " llegan
        // como textos distintos pero son el mismo ingrediente, y repetirlo en una
        // receta no significa nada.
        var distintos = normalizadas.Select(linea => linea.Nombre).Distinct().ToList();

        if (distintos.Count != normalizadas.Count)
        {
            return null;
        }

        var existentes = await ingredientes.BuscarPorNombresAsync(distintos, cancelacion);
        var porNombre = existentes.ToDictionary(ingrediente => ingrediente.Nombre);

        var aCrear = distintos
            .Where(nombre => !porNombre.ContainsKey(nombre))
            .Select(Ingrediente.Crear)
            .ToList();

        if (aCrear.Count > 0)
        {
            foreach (var creado in await ingredientes.AnadirNuevosAsync(aCrear, cancelacion))
            {
                porNombre[creado.Nombre] = creado;
            }
        }

        return normalizadas
            .Select(linea => (porNombre[linea.Nombre].Id, linea.Cantidad, linea.Unidad))
            .ToList();
    }
}
