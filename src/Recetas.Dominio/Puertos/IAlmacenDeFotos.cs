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
    /// Borra el archivo. No falla si ya no existía: el objetivo es que deje de
    /// estar, y que alguien se haya adelantado no es un error.
    /// </summary>
    Task BorrarAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default);
}
