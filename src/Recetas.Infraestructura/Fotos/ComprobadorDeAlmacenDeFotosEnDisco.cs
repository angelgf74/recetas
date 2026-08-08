using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Fotos;

/// <summary>
/// Diagnostica el directorio de fotos: existe, acepta escrituras y queda sitio.
/// </summary>
/// <remarks>
/// <b>Escribe de verdad para saber si se puede escribir.</b> Consultar permisos
/// con la API del sistema de archivos miente en cuanto hay listas de control de
/// acceso, un montaje de solo lectura o el disco lleno: los permisos dicen que sí
/// y la escritura falla igual. La única comprobación fiable es intentarlo.
/// </remarks>
public sealed class ComprobadorDeAlmacenDeFotosEnDisco(
    IOptions<OpcionesDeFotos> opciones,
    ILogger<ComprobadorDeAlmacenDeFotosEnDisco> registro) : IComprobadorDeAlmacenDeFotos
{
    private readonly string _directorio = Path.GetFullPath(opciones.Value.Directorio);
    private readonly long _minimoLibreEnBytes = opciones.Value.MinimoDeEspacioLibreEnMb * 1024 * 1024;

    public Task<bool> AceptaFotosAsync(CancellationToken cancelacion = default)
    {
        if (!Directory.Exists(_directorio))
        {
            registro.LogError("El directorio de fotos no existe: {Directorio}", _directorio);
            return Task.FromResult(false);
        }

        return Task.FromResult(HayEspacio() && SePuedeEscribir());
    }

    private bool HayEspacio()
    {
        try
        {
            var libre = new DriveInfo(Path.GetPathRoot(_directorio)!).AvailableFreeSpace;

            if (libre >= _minimoLibreEnBytes)
            {
                return true;
            }

            registro.LogError(
                "Queda poco espacio para las fotos: {LibreEnMb} MB libres, mínimo {MinimoEnMb} MB.",
                libre / (1024 * 1024),
                _minimoLibreEnBytes / (1024 * 1024));

            return false;
        }
        catch (Exception excepcion)
        {
            // En un montaje de red o dentro de un contenedor, DriveInfo puede no
            // saber responder. Se avisa de más antes que dar por bueno un disco
            // del que no se sabe nada.
            registro.LogError(excepcion, "No se pudo consultar el espacio libre de {Directorio}.", _directorio);
            return false;
        }
    }

    private bool SePuedeEscribir()
    {
        // En el propio directorio de fotos, no en el temporal del sistema:
        // comprobar /tmp no dice nada del disco que importa. Nombre reconocible
        // por si alguna vez queda uno huérfano y alguien se pregunta qué es.
        var prueba = Path.Combine(_directorio, $"salud-{Guid.NewGuid():N}.tmp");

        try
        {
            // De cero bytes: se llama cada pocos minutos y no tiene por qué
            // mover datos para demostrar que el disco acepta escrituras.
            using (File.Create(prueba))
            {
            }

            return true;
        }
        catch (Exception excepcion)
        {
            registro.LogError(excepcion, "No se puede escribir en el directorio de fotos {Directorio}.", _directorio);
            return false;
        }
        finally
        {
            // Siempre, también si algo falló por el camino: una sonda que deja
            // basura acaba llenando lo que vigila.
            try
            {
                File.Delete(prueba);
            }
            catch (IOException)
            {
            }
        }
    }
}
