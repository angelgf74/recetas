using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Dobles;

/// <summary>
/// Almacén de fotos en memoria. Permite comprobar que los archivos se escriben y
/// se borran cuando toca, sin tocar el disco.
/// </summary>
public sealed class AlmacenDeFotosEnMemoria : IAlmacenDeFotos
{
    private readonly Dictionary<Guid, byte[]> _archivos = [];
    private readonly Dictionary<Guid, byte[]> _miniaturas = [];

    public int Total => _archivos.Count;

    public int TotalDeMiniaturas => _miniaturas.Count;

    public bool Contiene(Guid fotoId) => _archivos.ContainsKey(fotoId);

    public bool ContieneMiniatura(Guid fotoId) => _miniaturas.ContainsKey(fotoId);

    /// <summary>
    /// Quita solo la miniatura, dejando el original. Es el estado en el que están
    /// las fotos subidas antes de la feature 009.
    /// </summary>
    public void OlvidarMiniatura(Guid fotoId) => _miniaturas.Remove(fotoId);

    /// <summary>Si es <c>true</c>, guardar falla: simula un disco lleno o sin permisos.</summary>
    public bool FallarAlGuardar { get; set; }

    public async Task GuardarAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream contenido,
        CancellationToken cancelacion = default)
    {
        if (FallarAlGuardar)
        {
            throw new IOException("Fallo simulado al escribir la foto.");
        }

        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancelacion);
        _archivos[fotoId] = memoria.ToArray();
    }

    public Task<Stream?> AbrirAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default) =>
        Task.FromResult<Stream?>(
            _archivos.TryGetValue(fotoId, out var bytes) ? new MemoryStream(bytes) : null);

    public async Task GuardarMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        Stream contenido,
        CancellationToken cancelacion = default)
    {
        if (FallarAlGuardar)
        {
            throw new IOException("Fallo simulado al escribir la miniatura.");
        }

        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancelacion);
        _miniaturas[fotoId] = memoria.ToArray();
    }

    public Task<Stream?> AbrirMiniaturaAsync(
        Guid fotoId,
        TipoDeImagen tipo,
        CancellationToken cancelacion = default) =>
        Task.FromResult<Stream?>(
            _miniaturas.TryGetValue(fotoId, out var bytes) ? new MemoryStream(bytes) : null);

    public Task BorrarAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default)
    {
        _archivos.Remove(fotoId);
        _miniaturas.Remove(fotoId);
        return Task.CompletedTask;
    }
}
