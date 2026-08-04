using Recetas.Dominio.Recetas;

namespace Recetas.Dominio.Puertos;

/// <summary>
/// Guarda y recupera los bytes de las fotos.
/// </summary>
/// <remarks>
/// Habla de identificadores, nunca de rutas: el dominio no sabe si detrás hay un
/// sistema de archivos, MinIO o S3. Cambiar de uno a otro es cambiar el adaptador.
/// </remarks>
public interface IAlmacenDeFotos
{
    Task GuardarAsync(Guid fotoId, TipoDeImagen tipo, Stream contenido, CancellationToken cancelacion = default);

    /// <summary>Devuelve el contenido, o <c>null</c> si el archivo no está.</summary>
    Task<Stream?> AbrirAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda la versión reducida de una foto.
    /// </summary>
    /// <remarks>
    /// La miniatura no tiene identificador propio: es otra representación de la
    /// misma foto, y el almacén la localiza con el identificador de esta. Así no
    /// hay dos cosas que puedan quedar apuntando a sitios distintos.
    /// </remarks>
    Task GuardarMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream contenido,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve la miniatura, o <c>null</c> si todavía no se ha generado.
    /// </summary>
    /// <remarks>
    /// Que falte es normal, no un error: las fotos subidas antes de la feature 009
    /// no la tienen hasta que alguien las pide por primera vez.
    /// </remarks>
    Task<Stream?> AbrirMiniaturaAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra el archivo <b>y su miniatura</b>. No falla si ya no existían: el
    /// objetivo es que dejen de estar, y que alguien se haya adelantado no es un
    /// error.
    /// </summary>
    Task BorrarAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default);
}
