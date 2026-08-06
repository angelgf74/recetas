using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <param name="Nombre">Nombre de la receta.</param>
/// <param name="TipoDePlato">Tipo de plato, de la lista cerrada.</param>
/// <param name="Elaboracion">Pasos de elaboración.</param>
/// <param name="Ingredientes">Líneas de ingrediente, al menos una.</param>
/// <param name="Raciones">Para cuántas raciones son las cantidades, o <c>null</c> si no se sabe.</param>
public sealed record DatosDeReceta(
    string Nombre,
    TipoDePlato TipoDePlato,
    string Elaboracion,
    IReadOnlyCollection<LineaDeIngrediente> Ingredientes,
    int? Raciones = null);

/// <summary>
/// Resultado de las operaciones que pueden fallar por permisos o por datos.
/// </summary>
/// <remarks>
/// <see cref="NoEncontrada"/> cubre a la vez "no existe" y "no es tuya". No es
/// pereza: distinguirlos permitiría averiguar qué identificadores corresponden a
/// recetas reales de otros usuarios, y la constitución exige responder 404 y no
/// 403 justo por eso.
/// </remarks>
public enum ResultadoDeReceta
{
    Correcto,
    NoEncontrada,
    DatosNoValidos
}

/// <summary>
/// Casos de uso del recetario propio: crear, consultar, editar y borrar.
/// </summary>
/// <remarks>
/// Todos reciben el identificador del autor como parámetro aparte, nunca dentro de
/// los datos de entrada: así el cliente no puede pedir que se cree o se lea algo
/// en nombre de otro usuario.
/// </remarks>
public sealed class GestionDeRecetas(
    IRepositorioDeRecetas recetas,
    ResolverIngredientes resolverIngredientes,
    IAlmacenDeFotos almacenDeFotos,
    IReloj reloj)
{
    public async Task<(ResultadoDeReceta Resultado, Receta? Receta)> CrearAsync(
        Guid autorId,
        DatosDeReceta datos,
        CancellationToken cancelacion = default)
    {
        var lineas = await resolverIngredientes.EjecutarAsync(datos.Ingredientes, cancelacion);

        if (lineas is null)
        {
            return (ResultadoDeReceta.DatosNoValidos, null);
        }

        Receta receta;

        try
        {
            // La visibilidad no se pasa: Receta.Crear la fija en Privada y no hay
            // forma de pedir otra cosa. Publicar llega con la feature 005.
            receta = Receta.Crear(
                autorId, datos.Nombre, datos.TipoDePlato, datos.Elaboracion, reloj.Ahora, datos.Raciones);
            receta.ReemplazarIngredientes(lineas);
        }
        catch (ArgumentException)
        {
            return (ResultadoDeReceta.DatosNoValidos, null);
        }

        await recetas.AnadirAsync(receta, cancelacion);

        return (ResultadoDeReceta.Correcto, receta);
    }

    public async Task<(ResultadoDeReceta Resultado, Receta? Receta)> ObtenerAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        // PuedeVerla, no EsDe: leer vale también si la receta está publicada.
        // Editar, borrar y publicar siguen exigiendo autoría, más abajo.
        //
        // Que no exista y que no se pueda ver comparten salida a propósito.
        return receta is null || !receta.PuedeVerla(usuarioId)
            ? (ResultadoDeReceta.NoEncontrada, null)
            : (ResultadoDeReceta.Correcto, receta);
    }

    public Task<IReadOnlyCollection<Receta>> ListarMiasAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default) =>
        recetas.ListarPorAutorAsync(usuarioId, cancelacion);

    /// <summary>Tope de resultados de una búsqueda.</summary>
    /// <remarks>
    /// Devolver una lista sin límite es un riesgo real: una búsqueda sin criterios
    /// traería todo lo visible. Recorrer páginas todavía no le hace falta a nadie,
    /// así que se acota y se avisa del recorte, que es la mitad barata del
    /// problema. La paginación entra cuando este tope moleste.
    /// </remarks>
    public const int MaximoDeResultados = 50;

    public async Task<(IReadOnlyList<Receta> Resultados, bool HayMas)> BuscarAsync(
        Guid usuarioId,
        CriteriosDeBusqueda criterios,
        CancellationToken cancelacion = default)
    {
        // El repositorio devuelve uno más que el tope justo para poder decirlo.
        var encontradas = await recetas.BuscarAsync(usuarioId, criterios, MaximoDeResultados, cancelacion);

        var hayMas = encontradas.Count > MaximoDeResultados;

        return (encontradas.Take(MaximoDeResultados).ToList(), hayMas);
    }

    public async Task<ResultadoDeReceta> ActualizarAsync(
        Guid usuarioId,
        Guid recetaId,
        DatosDeReceta datos,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.EsDe(usuarioId))
        {
            return ResultadoDeReceta.NoEncontrada;
        }

        var lineas = await resolverIngredientes.EjecutarAsync(datos.Ingredientes, cancelacion);

        if (lineas is null)
        {
            return ResultadoDeReceta.DatosNoValidos;
        }

        try
        {
            receta.Actualizar(
                datos.Nombre, datos.TipoDePlato, datos.Elaboracion, reloj.Ahora, datos.Raciones);
            receta.ReemplazarIngredientes(lineas);
        }
        catch (ArgumentException)
        {
            return ResultadoDeReceta.DatosNoValidos;
        }

        await recetas.GuardarCambiosAsync(cancelacion);

        return ResultadoDeReceta.Correcto;
    }

    /// <summary>
    /// Cambia la visibilidad de una receta propia.
    /// </summary>
    /// <remarks>
    /// Exige autoría (<c>EsDe</c>), no solo poder verla: si aceptara
    /// <c>PuedeVerla</c>, cualquier usuario podría despublicar las recetas
    /// públicas de los demás.
    /// </remarks>
    public async Task<ResultadoDeReceta> CambiarVisibilidadAsync(
        Guid usuarioId,
        Guid recetaId,
        bool publicar,
        bool esResponsable = false,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null)
        {
            return ResultadoDeReceta.NoEncontrada;
        }

        // Retirada por moderación: el responsable del servicio puede devolver a
        // privada una receta pública ajena, y NADA MÁS.
        //
        // Las dos restricciones importan. Solo retirar, porque publicar una receta
        // privada ajena expondría contenido que su autor nunca compartió; y solo
        // sobre lo ya público, porque sobre lo privado el responsable no tiene por
        // qué actuar: nadie puede haberlo denunciado.
        //
        // Esta es una tercera pregunta, distinta de EsDe y de PuedeVerla, y llega
        // hasta aquí como parámetro explícito precisamente para que no se cuele en
        // editar ni en borrar.
        var esRetiradaPorModeracion = esResponsable && !publicar && receta.EsPublica;

        if (!receta.EsDe(usuarioId) && !esRetiradaPorModeracion)
        {
            return ResultadoDeReceta.NoEncontrada;
        }

        if (publicar)
        {
            receta.Publicar(reloj.Ahora);
        }
        else
        {
            receta.Despublicar(reloj.Ahora);
        }

        await recetas.GuardarCambiosAsync(cancelacion);

        return ResultadoDeReceta.Correcto;
    }

    public async Task<ResultadoDeReceta> BorrarAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.EsDe(usuarioId))
        {
            return ResultadoDeReceta.NoEncontrada;
        }

        // Los archivos primero, las filas después.
        //
        // La cascada de la base de datos borra las filas de las fotos, pero no
        // toca el disco: hacerlo al revés dejaría archivos que ninguna fila
        // menciona, invisibles y ocupando espacio para siempre. En este orden, un
        // fallo deja la receta intacta y la operación se puede repetir.
        //
        // Que un archivo concreto ya no esté no aborta el borrado: el almacén
        // trata "ya no existe" como éxito, porque el objetivo es que deje de estar.
        foreach (var foto in receta.Fotos)
        {
            await almacenDeFotos.BorrarAsync(foto.Id, foto.Tipo, cancelacion);
        }

        await recetas.BorrarAsync(receta, cancelacion);

        return ResultadoDeReceta.Correcto;
    }
}
