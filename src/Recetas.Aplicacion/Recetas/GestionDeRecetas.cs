using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

/// <param name="Nombre">Nombre de la receta.</param>
/// <param name="TipoDePlato">Tipo de plato, de la lista cerrada.</param>
/// <param name="Elaboracion">Pasos de elaboración.</param>
/// <param name="Ingredientes">Líneas de ingrediente, al menos una.</param>
public sealed record DatosDeReceta(
    string Nombre,
    TipoDePlato TipoDePlato,
    string Elaboracion,
    IReadOnlyCollection<LineaDeIngrediente> Ingredientes);

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
            receta = Receta.Crear(autorId, datos.Nombre, datos.TipoDePlato, datos.Elaboracion, reloj.Ahora);
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

        // Que no exista y que sea de otro comparten salida a propósito.
        return receta is null || !receta.EsDe(usuarioId)
            ? (ResultadoDeReceta.NoEncontrada, null)
            : (ResultadoDeReceta.Correcto, receta);
    }

    public Task<IReadOnlyCollection<Receta>> ListarMiasAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default) =>
        recetas.ListarPorAutorAsync(usuarioId, cancelacion);

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
            receta.Actualizar(datos.Nombre, datos.TipoDePlato, datos.Elaboracion, reloj.Ahora);
            receta.ReemplazarIngredientes(lineas);
        }
        catch (ArgumentException)
        {
            return ResultadoDeReceta.DatosNoValidos;
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
