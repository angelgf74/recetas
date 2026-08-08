using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Fotos;

/// <summary>
/// Guarda las fotos como archivos sueltos en una carpeta del servidor.
/// </summary>
/// <remarks>
/// El nombre del archivo se construye <b>solo</b> con el identificador de la foto
/// y su extensión. Ningún texto del cliente participa en la ruta, así que no hay
/// superficie para una travesía de directorios.
/// </remarks>
public sealed class AlmacenDeFotosEnDisco : IAlmacenDeFotos
{
    private readonly string _directorio;
    private readonly ILogger<AlmacenDeFotosEnDisco> _registro;

    public AlmacenDeFotosEnDisco(IOptions<OpcionesDeFotos> opciones, ILogger<AlmacenDeFotosEnDisco> registro)
    {
        _directorio = Path.GetFullPath(opciones.Value.Directorio);
        _registro = registro;

        // Crear al arrancar y no en cada subida: si la carpeta no existe o no es
        // escribible, conviene que falle aquí y no en la primera foto de un usuario.
        Directory.CreateDirectory(_directorio);
    }

    public async Task GuardarAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream contenido,
        CancellationToken cancelacion = default)
    {
        await using var archivo = File.Create(Ruta(fotoId, tipo));
        await contenido.CopyToAsync(archivo, cancelacion);
    }

    public Task<Stream?> AbrirAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default)
    {
        var ruta = Ruta(fotoId, tipo);

        if (!File.Exists(ruta))
        {
            // Fila sin archivo. Se registra porque significa que las dos mitades
            // del almacenamiento se han desincronizado, y eso no debería ocurrir.
            _registro.LogWarning("La foto {FotoId} está en la base de datos pero no en disco.", fotoId);
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(Leer(ruta));
    }

    public async Task GuardarMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream contenido,
        CancellationToken cancelacion = default)
    {
        await using var archivo = File.Create(RutaDeMiniatura(fotoId, tipo));
        await contenido.CopyToAsync(archivo, cancelacion);
    }

    public Task<Stream?> AbrirMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        CancellationToken cancelacion = default)
    {
        var ruta = RutaDeMiniatura(fotoId, tipo);

        // Aquí NO se registra un aviso cuando falta: que no esté es lo normal para
        // las fotos subidas antes de la feature 009, y quien llama la genera.
        return Task.FromResult<Stream?>(File.Exists(ruta) ? Leer(ruta) : null);
    }

    public Task BorrarAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default)
    {
        try
        {
            // Las dos, y en este orden da igual: File.Delete no falla si el archivo
            // no existe, que es justo el comportamiento que pide el puerto.
            File.Delete(Ruta(fotoId, tipo));
            File.Delete(RutaDeMiniatura(fotoId, tipo));
        }
        catch (IOException excepcion)
        {
            // No se propaga: dejar de borrar una receta porque un archivo está
            // bloqueado sería peor para el usuario que un archivo de más en disco.
            _registro.LogError(excepcion, "No se pudo borrar el archivo de la foto {FotoId}.", fotoId);
        }

        return Task.CompletedTask;
    }

    private static FileStream Leer(string ruta) =>
        new(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    private string Ruta(Guid fotoId, TipoDeImagen tipo) =>
        Path.Combine(_directorio, $"{fotoId:N}.{tipo.Extension()}");

    /// <summary>
    /// La miniatura vive junto al original, con sufijo. No lleva identificador
    /// propio: es otra representación de la misma foto, y derivar su ruta impide
    /// que las dos puedan quedar apuntando a sitios distintos.
    /// </summary>
    private string RutaDeMiniatura(Guid fotoId, TipoDeImagen tipo) =>
        Path.Combine(_directorio, $"{fotoId:N}-min.{tipo.Extension()}");
}
