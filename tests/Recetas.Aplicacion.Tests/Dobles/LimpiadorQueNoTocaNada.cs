using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Dobles;

/// <summary>
/// Limpiador que devuelve el contenido tal cual.
/// </summary>
/// <remarks>
/// Los tests de aplicación comprueban el flujo (que se limpia antes de guardar,
/// que un contenido irrecuperable se rechaza), no la limpieza en sí: eso se
/// verifica con imágenes reales en <c>Recetas.Infraestructura.Tests</c>.
/// </remarks>
public sealed class LimpiadorQueNoTocaNada : ILimpiadorDeImagenes
{
    /// <summary>Si es <c>true</c>, la limpieza falla: simula una imagen corrupta.</summary>
    public bool Rechazar { get; set; }

    public async Task<Stream?> LimpiarAsync(
        Stream original,
        TipoDeImagen tipo,
        CancellationToken cancelacion = default)
    {
        if (Rechazar)
        {
            return null;
        }

        var copia = new MemoryStream();
        await original.CopyToAsync(copia, cancelacion);
        copia.Position = 0;

        return copia;
    }
}
