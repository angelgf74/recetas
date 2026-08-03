using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Recetas;

public enum ResultadoDeFoto
{
    Correcto,

    /// <summary>La receta o la foto no existen, o no son de quien pregunta.</summary>
    NoEncontrada,

    /// <summary>El archivo no es una imagen de un formato admitido, o está vacío.</summary>
    NoEsUnaImagen,

    DemasiadoGrande
}

/// <param name="Contenido">Bytes de la imagen ya en memoria.</param>
/// <param name="Tipo">Formato deducido del contenido.</param>
public sealed record FotoDescargada(Stream Contenido, TipoDeImagen Tipo);

/// <summary>
/// Subida, consulta y borrado de fotos de receta.
/// </summary>
/// <remarks>
/// Una foto no tiene permisos propios: los hereda de su receta. Por eso todas las
/// operaciones empiezan localizando la receta y comprobando la autoría, igual que
/// en la feature 003.
/// </remarks>
public sealed class GestionDeFotos(
    IRepositorioDeRecetas recetas,
    IAlmacenDeFotos almacen,
    IReloj reloj)
{
    public async Task<(ResultadoDeFoto Resultado, Foto? Foto)> SubirAsync(
        Guid usuarioId,
        Guid recetaId,
        Stream contenido,
        long tamanoMaximoEnBytes,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.EsDe(usuarioId))
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        // Se copia a memoria para poder mirar la cabecera y, después, escribir
        // desde el principio. El límite de tamaño lo hace acotado.
        using var enMemoria = new MemoryStream();
        await contenido.CopyToAsync(enMemoria, cancelacion);

        if (enMemoria.Length == 0)
        {
            return (ResultadoDeFoto.NoEsUnaImagen, null);
        }

        if (enMemoria.Length > tamanoMaximoEnBytes)
        {
            return (ResultadoDeFoto.DemasiadoGrande, null);
        }

        var cabecera = enMemoria.GetBuffer()
            .AsSpan(0, (int)Math.Min(DetectorDeImagen.BytesNecesarios, enMemoria.Length));

        if (!DetectorDeImagen.TryDetectar(cabecera, out var tipo))
        {
            return (ResultadoDeFoto.NoEsUnaImagen, null);
        }

        var foto = receta.AnadirFoto(tipo, enMemoria.Length, reloj.Ahora);

        // La fila primero y el archivo después.
        //
        // Al revés, un fallo al guardar la fila dejaría bytes en disco que ninguna
        // fila menciona: invisibles y ocupando espacio para siempre. En este orden,
        // el caso malo es una fila sin archivo, que da un error visible al leerla.
        await recetas.GuardarCambiosAsync(cancelacion);

        try
        {
            enMemoria.Position = 0;
            await almacen.GuardarAsync(foto.Id, tipo, enMemoria, cancelacion);
        }
        catch (Exception)
        {
            // Si el archivo no ha podido escribirse, se deshace la fila para no
            // dejar una foto que no se puede descargar.
            receta.QuitarFoto(foto.Id, reloj.Ahora);
            await recetas.GuardarCambiosAsync(CancellationToken.None);
            throw;
        }

        return (ResultadoDeFoto.Correcto, foto);
    }

    public async Task<(ResultadoDeFoto Resultado, FotoDescargada? Foto)> ObtenerAsync(
        Guid usuarioId,
        Guid recetaId,
        Guid fotoId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.EsDe(usuarioId))
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        var foto = receta.Fotos.FirstOrDefault(f => f.Id == fotoId);

        if (foto is null)
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        var contenido = await almacen.AbrirAsync(foto.Id, foto.Tipo, cancelacion);

        // Fila sin archivo: se responde como si no existiera. Es el caso que el
        // orden de escritura deja posible, y no hay nada que servir.
        return contenido is null
            ? (ResultadoDeFoto.NoEncontrada, null)
            : (ResultadoDeFoto.Correcto, new FotoDescargada(contenido, foto.Tipo));
    }

    public async Task<ResultadoDeFoto> BorrarAsync(
        Guid usuarioId,
        Guid recetaId,
        Guid fotoId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        if (receta is null || !receta.EsDe(usuarioId))
        {
            return ResultadoDeFoto.NoEncontrada;
        }

        var foto = receta.QuitarFoto(fotoId, reloj.Ahora);

        if (foto is null)
        {
            return ResultadoDeFoto.NoEncontrada;
        }

        await recetas.GuardarCambiosAsync(cancelacion);
        await almacen.BorrarAsync(foto.Id, foto.Tipo, cancelacion);

        return ResultadoDeFoto.Correcto;
    }
}
