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
    ILimpiadorDeImagenes limpiador,
    IEscaladorDeImagenes escalador,
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

        // Se limpian los metadatos ANTES de que los bytes lleguen al disco. Los
        // móviles incrustan la ubicación GPS en el EXIF, y publicar una receta con
        // la foto intacta expondría la dirección de quien la cocinó.
        enMemoria.Position = 0;
        using var limpia = await limpiador.LimpiarAsync(enMemoria, tipo, cancelacion);

        if (limpia is null)
        {
            // Cabecera de imagen válida pero contenido que no se puede decodificar.
            return (ResultadoDeFoto.NoEsUnaImagen, null);
        }

        // El tamaño que se registra es el del archivo que realmente se guarda, no
        // el que subió el cliente: al recodificar cambia.
        var foto = receta.AnadirFoto(tipo, limpia.Length, reloj.Ahora);

        // La fila primero y el archivo después.
        //
        // Al revés, un fallo al guardar la fila dejaría bytes en disco que ninguna
        // fila menciona: invisibles y ocupando espacio para siempre. En este orden,
        // el caso malo es una fila sin archivo, que da un error visible al leerla.
        await recetas.GuardarCambiosAsync(cancelacion);

        try
        {
            limpia.Position = 0;
            await almacen.GuardarAsync(foto.Id, tipo, limpia, cancelacion);

            // La miniatura sale de la imagen ya limpia, nunca del archivo que
            // subió el cliente: es la copia que más se va a ver, y no debe
            // heredar ni metadatos ni una orientación sin aplicar.
            limpia.Position = 0;
            await GuardarMiniaturaAsync(foto.Id, tipo, limpia, cancelacion);
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

        // PuedeVerla: las fotos heredan la visibilidad de su receta, así que las
        // de una receta publicada las puede descargar cualquier usuario registrado.
        // Subir y borrar, más abajo, siguen exigiendo autoría.
        if (receta is null || !receta.PuedeVerla(usuarioId))
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

    /// <summary>
    /// Versión reducida de una foto, para los listados.
    /// </summary>
    /// <remarks>
    /// Si la miniatura no está en disco, se genera aquí y se guarda. Es lo que
    /// hace que las fotos subidas antes de la feature 009 acaben teniéndola sin
    /// necesidad de un script de relleno: la primera petición la crea y las
    /// siguientes ya la encuentran.
    /// </remarks>
    public async Task<(ResultadoDeFoto Resultado, FotoDescargada? Foto)> ObtenerMiniaturaAsync(
        Guid usuarioId,
        Guid recetaId,
        Guid fotoId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        // Mismas reglas que la foto completa: una miniatura de una receta privada
        // es tan privada como la foto de la que sale.
        if (receta is null || !receta.PuedeVerla(usuarioId))
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        var foto = receta.Fotos.FirstOrDefault(f => f.Id == fotoId);

        if (foto is null)
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        var miniatura = await almacen.AbrirMiniaturaAsync(foto.Id, foto.Tipo, cancelacion);

        if (miniatura is not null)
        {
            return (ResultadoDeFoto.Correcto, new FotoDescargada(miniatura, foto.Tipo));
        }

        var original = await almacen.AbrirAsync(foto.Id, foto.Tipo, cancelacion);

        // Sin original no hay nada que escalar. Se responde como la descarga
        // completa en el mismo caso: como si la foto no existiera.
        if (original is null)
        {
            return (ResultadoDeFoto.NoEncontrada, null);
        }

        await using (original)
        {
            var generada = await GuardarMiniaturaAsync(foto.Id, foto.Tipo, original, cancelacion);

            if (generada is null)
            {
                // El original está en disco pero no se puede decodificar. No es
                // recuperable desde aquí, y servir el archivo entero en su lugar
                // sería justo lo que esta feature evita.
                return (ResultadoDeFoto.NoEncontrada, null);
            }

            generada.Position = 0;
            return (ResultadoDeFoto.Correcto, new FotoDescargada(generada, foto.Tipo));
        }
    }

    /// <summary>
    /// Escala y guarda la miniatura. Devuelve lo escalado, o <c>null</c> si la
    /// imagen no se ha podido decodificar.
    /// </summary>
    private async Task<Stream?> GuardarMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream imagen,
        CancellationToken cancelacion)
    {
        var miniatura = await escalador.EscalarAsync(
            imagen, tipo, IEscaladorDeImagenes.AnchoDeMiniatura, cancelacion);

        if (miniatura is null)
        {
            return null;
        }

        miniatura.Position = 0;
        await almacen.GuardarMiniaturaAsync(fotoId, tipo, miniatura, cancelacion);

        return miniatura;
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
