namespace Recetas.Dominio.Recetas;

/// <summary>
/// Una foto de receta. En la base de datos vive solo esta referencia; los bytes
/// están en el almacén de archivos.
/// </summary>
/// <remarks>
/// <b>No guarda el nombre del archivo original.</b> La ruta en disco se deriva del
/// <see cref="Id"/> y del tipo de imagen, así que ningún texto del usuario llega a
/// tocar el sistema de archivos: un nombre de archivo controlado por el cliente es
/// la vía clásica de travesía de rutas.
/// </remarks>
public sealed class Foto
{
    private Foto(Guid id, Guid recetaId, TipoDeImagen tipo, long tamanoEnBytes, DateTimeOffset ahora)
    {
        Id = id;
        RecetaId = recetaId;
        Tipo = tipo;
        TamanoEnBytes = tamanoEnBytes;
        FechaDeSubida = ahora;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Foto()
    {
    }

    public Guid Id { get; private set; }

    public Guid RecetaId { get; private set; }

    public TipoDeImagen Tipo { get; private set; }

    public long TamanoEnBytes { get; private set; }

    public DateTimeOffset FechaDeSubida { get; private set; }

    public static Foto Crear(Guid recetaId, TipoDeImagen tipo, long tamanoEnBytes, DateTimeOffset ahora)
    {
        if (tamanoEnBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tamanoEnBytes), "Una foto vacía no es una foto.");
        }

        return new Foto(Guid.NewGuid(), recetaId, tipo, tamanoEnBytes, ahora);
    }
}
