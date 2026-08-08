namespace Recetas.Dominio.Puertos;

/// <summary>
/// Puerto de salida que responde si el almacenamiento de fotos puede recibirlas.
/// </summary>
/// <remarks>
/// Separado de <see cref="IAlmacenDeFotos"/> a propósito: uno guarda y lee fotos,
/// este diagnostica si eso va a ser posible. Si algún día el almacén pasa a S3, el
/// diagnóstico deja de hablar de espacio en disco y pasa a hablar de la cuenta,
/// y conviene poder cambiar una cosa sin tocar la otra.
/// </remarks>
public interface IComprobadorDeAlmacenDeFotos
{
    /// <summary>
    /// Si el almacenamiento está listo para recibir fotos: existe, acepta
    /// escrituras y queda sitio.
    /// </summary>
    Task<bool> AceptaFotosAsync(CancellationToken cancelacion = default);
}
