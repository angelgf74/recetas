using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <summary>
/// Traduce etiquetas escritas por el usuario a identificadores del catálogo,
/// creando las que aún no existan.
/// </summary>
/// <remarks>
/// Calco de <see cref="ResolverIngredientes"/>. Es el único punto donde se dan
/// de alta etiquetas nuevas, lo que mantiene el catálogo limpio para la
/// búsqueda.
/// </remarks>
public sealed class ResolverEtiquetas(IRepositorioDeEtiquetas etiquetas)
{
    /// <summary>
    /// Devuelve <c>null</c> si alguna etiqueta no es un nombre válido o si se
    /// supera <see cref="Receta.MaximoDeEtiquetas"/>: mismo contrato que
    /// <see cref="ResolverIngredientes.EjecutarAsync"/>, para que
    /// <c>GestionDeRecetas</c> lo trate como datos no válidos sin lógica nueva.
    /// </summary>
    public async Task<IReadOnlyList<Guid>?> EjecutarAsync(
        IReadOnlyCollection<string> nombresIndicados,
        CancellationToken cancelacion = default)
    {
        if (nombresIndicados.Count == 0)
        {
            return [];
        }

        var normalizados = new List<NombreDeEtiqueta>(nombresIndicados.Count);

        foreach (var indicado in nombresIndicados)
        {
            if (!NombreDeEtiqueta.TryCrear(indicado, out var nombre))
            {
                return null;
            }

            normalizados.Add(nombre);
        }

        // Se comprueba tras normalizar: "Rápido" y " rápido " llegan como
        // textos distintos pero son la misma etiqueta.
        var distintos = normalizados.Distinct().ToList();

        if (distintos.Count > Receta.MaximoDeEtiquetas)
        {
            return null;
        }

        var existentes = await etiquetas.BuscarPorNombresAsync(distintos, cancelacion);
        var porNombre = existentes.ToDictionary(etiqueta => etiqueta.Nombre);

        var aCrear = distintos
            .Where(nombre => !porNombre.ContainsKey(nombre))
            .Select(Etiqueta.Crear)
            .ToList();

        if (aCrear.Count > 0)
        {
            foreach (var creada in await etiquetas.AnadirNuevasAsync(aCrear, cancelacion))
            {
                porNombre[creada.Nombre] = creada;
            }
        }

        return distintos.Select(nombre => porNombre[nombre].Id).ToList();
    }
}
