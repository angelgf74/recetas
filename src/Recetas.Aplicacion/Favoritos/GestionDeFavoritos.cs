using Recetas.Dominio.Favoritos;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Favoritos;

/// <summary>
/// Resultado de marcar una receta.
/// </summary>
/// <remarks>
/// <see cref="NoEncontrada"/> cubre "no existe" y "es privada y no es tuya", por
/// la razón de siempre: distinguirlas diría qué recetas privadas existen.
/// </remarks>
public enum ResultadoDeFavorito
{
    Correcto,
    NoEncontrada
}

/// <summary>
/// Favoritos privados: marcar una receta para volver a encontrarla.
/// </summary>
/// <remarks>
/// <b>Solo hay una pregunta de permisos aquí, y es <c>PuedeVerla</c></b>: se marca
/// lo propio y lo ajeno publicado. Marcar no modifica la receta —ni su fecha de
/// modificación—, así que no hace falta ser su autor ni nada parecido.
/// <para>
/// Nada de lo que hay aquí es visible para el autor de la receta. No existe
/// recuento ni consulta de "quién la ha marcado", y el hueco es deliberado:
/// <c>mission.md</c> descarta las valoraciones porque hacen competir a las
/// recetas entre sí, y un contador de favoritos sería lo mismo con otro nombre.
/// </para>
/// </remarks>
public sealed class GestionDeFavoritos(
    IRepositorioDeRecetas recetas,
    IRepositorioDeFavoritos favoritos,
    IReloj reloj)
{
    public async Task<ResultadoDeFavorito> MarcarAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.PuedeVerla(usuarioId))
        {
            return ResultadoDeFavorito.NoEncontrada;
        }

        // Marcar dos veces es el mismo estado final. La clave primaria compuesta
        // cubre además las dos peticiones simultáneas que esta comprobación no
        // puede ver.
        if (await favoritos.EstaMarcadaAsync(usuarioId, recetaId, cancelacion))
        {
            return ResultadoDeFavorito.Correcto;
        }

        await favoritos.AnadirAsync(Favorito.Crear(usuarioId, recetaId, reloj.Ahora), cancelacion);
        await favoritos.GuardarCambiosAsync(cancelacion);

        return ResultadoDeFavorito.Correcto;
    }

    /// <summary>
    /// Quita la marca.
    /// </summary>
    /// <remarks>
    /// <b>No comprueba la visibilidad, y no es un descuido.</b> Si el autor
    /// despublica, su receta desaparece de la lista pero la marca sigue ahí;
    /// exigir <c>PuedeVerla</c> para quitarla dejaría una fila que el usuario no
    /// tiene forma de eliminar. Tampoco hay nada que proteger: se borra algo suyo.
    /// </remarks>
    public async Task DesmarcarAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        await favoritos.QuitarAsync(usuarioId, recetaId, cancelacion);
        await favoritos.GuardarCambiosAsync(cancelacion);
    }

    /// <summary>Mis favoritas que todavía puedo ver, de la última marcada a la primera.</summary>
    public Task<IReadOnlyCollection<Receta>> ListarMisFavoritasAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default) =>
        recetas.ListarFavoritasAsync(usuarioId, cancelacion);

    public Task<bool> EsFavoritaAsync(
        Guid usuarioId,
        Guid recetaId,
        CancellationToken cancelacion = default) =>
        favoritos.EstaMarcadaAsync(usuarioId, recetaId, cancelacion);
}
