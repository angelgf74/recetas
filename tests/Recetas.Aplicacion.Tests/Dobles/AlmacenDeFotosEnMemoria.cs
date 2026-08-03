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

    public int Total => _archivos.Count;

    public bool Contiene(Guid fotoId) => _archivos.ContainsKey(fotoId);

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

    public Task BorrarAsync(Guid fotoId, TipoDeImagen tipo, CancellationToken cancelacion = default)
    {
        _archivos.Remove(fotoId);
        return Task.CompletedTask;
    }
}
