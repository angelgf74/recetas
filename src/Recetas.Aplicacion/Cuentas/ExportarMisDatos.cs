using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Cuentas;

/// <param name="Correo">Correo de la cuenta.</param>
/// <param name="FechaDeAlta">Cuándo se creó.</param>
/// <param name="Recetas">Sus recetas, con ingredientes y fotos.</param>
public sealed record DatosDelUsuario(
    string Correo,
    DateTimeOffset FechaDeAlta,
    IReadOnlyList<Receta> Recetas);

/// <summary>
/// Reúne todo lo que el producto guarda de un usuario, para que pueda
/// llevárselo.
/// </summary>
/// <remarks>
/// Devuelve las entidades y ofrece abrir cada foto, pero <b>no construye el
/// paquete</b>: en qué formato se le entrega al usuario es decisión del
/// adaptador de salida, igual que lo es que las recetas viajen como JSON.
/// <para>
/// <b>No incluye el hash de la contraseña.</b> No le sirve de nada a quien
/// exporta y es justo el dato que no conviene tener en un archivo que acabará en
/// una carpeta de descargas.
/// </para>
/// </remarks>
public sealed class ExportarMisDatos(
    IRepositorioDeUsuarios usuarios,
    IRepositorioDeRecetas recetas,
    IAlmacenDeFotos almacenDeFotos)
{
    /// <summary>Devuelve <c>null</c> si la cuenta ya no existe.</summary>
    public async Task<DatosDelUsuario?> EjecutarAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, cancelacion);

        if (usuario is null)
        {
            return null;
        }

        // Solo las suyas. Ni siquiera las públicas de otros, que puede ver pero
        // no son datos suyos.
        //
        // Y completas: el listado normal omite los ingredientes porque el
        // recetario no los pinta, y exportar una receta sin ingredientes sería
        // entregar media receta sin avisar.
        var suyas = await recetas.ListarCompletasPorAutorAsync(usuarioId, cancelacion);

        return new DatosDelUsuario(
            usuario.Correo.Valor,
            usuario.FechaDeAlta,
            [.. suyas.OrderBy(receta => receta.FechaDeCreacion)]);
    }

    /// <summary>
    /// Abre el contenido de una foto, o <c>null</c> si el archivo ya no está.
    /// </summary>
    /// <remarks>
    /// Que falte un archivo no debe abortar la exportación: es mejor entregar el
    /// resto que negarle a alguien todos sus datos por una foto perdida.
    /// </remarks>
    public Task<Stream?> AbrirFotoAsync(
        Foto foto,
        CancellationToken cancelacion = default) =>
        almacenDeFotos.AbrirAsync(foto.Id, foto.Tipo, cancelacion);
}
