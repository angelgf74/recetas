using System.Text;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Recetas;

namespace Recetas.Aplicacion.Tests.Dobles;

/// <summary>
/// Escalador que no escala: devuelve una marca reconocible.
/// </summary>
/// <remarks>
/// Los tests de aplicación comprueban el flujo —que la miniatura se genera al
/// subir, que se crea sola si falta, que se borra con la foto—, no el escalado en
/// sí: eso se verifica con imágenes reales en <c>Recetas.Infraestructura.Tests</c>.
/// <para>
/// Devolver bytes distintos del original es lo que permite afirmar que lo servido
/// es la miniatura y no el archivo entero.
/// </para>
/// </remarks>
public sealed class EscaladorFalso : IEscaladorDeImagenes
{
    public static readonly byte[] Marca = Encoding.ASCII.GetBytes("MINIATURA");

    /// <summary>Si es <c>true</c>, escalar falla: simula una imagen corrupta.</summary>
    public bool Rechazar { get; set; }

    /// <summary>Cuántas veces se ha pedido escalar. Delata una regeneración de más.</summary>
    public int Llamadas { get; private set; }

    public int UltimoAnchoPedido { get; private set; }

    public Task<Stream?> EscalarAsync(
        Stream original,
        TipoDeImagen tipo,
        int anchoMaximo,
        CancellationToken cancelacion = default)
    {
        Llamadas++;
        UltimoAnchoPedido = anchoMaximo;

        return Task.FromResult<Stream?>(Rechazar ? null : new MemoryStream(Marca));
    }
}
