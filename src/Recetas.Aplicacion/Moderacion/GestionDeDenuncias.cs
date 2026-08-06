using Microsoft.Extensions.Logging;
using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Aplicacion.Moderacion;

/// <summary>
/// Resultado de denunciar.
/// </summary>
/// <remarks>
/// <see cref="NoEncontrada"/> cubre "no existe" y "es privada y no es tuya", por
/// la misma razón que en el resto de la API: distinguirlas convertiría el
/// endpoint en una forma de averiguar qué recetas privadas existen.
/// </remarks>
public enum ResultadoDeDenuncia
{
    Correcto,
    NoEncontrada,

    /// <summary>Nadie denuncia su propia receta. Es un error de uso, no de permisos.</summary>
    EsPropia
}

/// <summary>
/// Denunciar una receta pública ajena.
/// </summary>
/// <remarks>
/// La denuncia se <b>guarda primero</b> y se avisa después. Si el correo falla, el
/// aviso se pierde pero la denuncia existe: al revés, un buzón caído borraría la
/// única constancia de que alguien se quejó.
/// </remarks>
public sealed class GestionDeDenuncias(
    IRepositorioDeRecetas recetas,
    IRepositorioDeDenuncias denuncias,
    IEnviadorDeCorreo enviadorDeCorreo,
    CorreoDelResponsable responsable,
    IReloj reloj,
    ILogger<GestionDeDenuncias> registro)
{
    public async Task<ResultadoDeDenuncia> DenunciarAsync(
        Guid denuncianteId,
        Guid recetaId,
        MotivoDeDenuncia motivo,
        string? comentario,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        // El orden no es intercambiable: primero si puede verla, para no revelar
        // nada sobre recetas privadas ajenas, y solo después si es suya.
        if (receta is null || !receta.PuedeVerla(denuncianteId))
        {
            return ResultadoDeDenuncia.NoEncontrada;
        }

        if (receta.EsDe(denuncianteId))
        {
            return ResultadoDeDenuncia.EsPropia;
        }

        // Denunciar dos veces lo mismo no es un error: al usuario le da igual y a
        // efectos prácticos ya está hecho. Simplemente no se vuelve a molestar al
        // responsable con un aviso que ya tiene.
        if (await denuncias.YaDenuncioAsync(denuncianteId, recetaId, cancelacion))
        {
            return ResultadoDeDenuncia.Correcto;
        }

        var denuncia = Denuncia.Crear(recetaId, denuncianteId, motivo, comentario, reloj.Ahora);

        await denuncias.AnadirAsync(denuncia, cancelacion);
        await denuncias.GuardarCambiosAsync(cancelacion);

        await AvisarAlResponsableAsync(receta.Nombre, denuncia, cancelacion);

        return ResultadoDeDenuncia.Correcto;
    }

    private async Task AvisarAlResponsableAsync(
        string nombreDeLaReceta,
        Denuncia denuncia,
        CancellationToken cancelacion)
    {
        if (!CorreoElectronico.TryCrear(responsable.Valor, out var destinatario))
        {
            registro.LogWarning(
                "Denuncia {Denuncia} registrada sin avisar: no hay un correo de responsable válido configurado.",
                denuncia.Id);

            return;
        }

        var aviso = new AvisoDeDenuncia(
            denuncia.RecetaId,
            nombreDeLaReceta,
            denuncia.Motivo.ToString(),
            denuncia.Comentario);

        try
        {
            await enviadorDeCorreo.EnviarAvisoDeDenunciaAsync(destinatario, aviso, cancelacion);
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            // La denuncia ya está guardada. Que el aviso no salga es un problema
            // de operación, no del usuario, y devolverle un error le haría pensar
            // que su denuncia no ha llegado a ninguna parte.
            registro.LogError(
                excepcion,
                "No se pudo avisar de la denuncia {Denuncia} sobre la receta {Receta}.",
                denuncia.Id,
                denuncia.RecetaId);
        }
    }
}

/// <summary>
/// Correo del responsable del servicio, el que recibe los avisos de denuncia y el
/// único que puede retirar contenido publicado.
/// </summary>
/// <remarks>
/// Es configuración, no un dato del dominio: hay exactamente un responsable y no
/// se prevé un segundo. Un campo <c>EsAdministrador</c> en <c>usuarios</c> pediría
/// a gritos una gestión de roles alrededor, para una lista de un elemento.
/// </remarks>
public sealed record CorreoDelResponsable(string? Valor)
{
    /// <summary>
    /// Si ese correo es el del responsable. Comparación sin distinguir mayúsculas
    /// porque los correos se guardan normalizados en minúsculas.
    /// </summary>
    public bool Es(string? correo) =>
        !string.IsNullOrWhiteSpace(Valor)
        && !string.IsNullOrWhiteSpace(correo)
        && string.Equals(Valor.Trim(), correo.Trim(), StringComparison.OrdinalIgnoreCase);
}
