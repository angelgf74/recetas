using Microsoft.Extensions.Logging;
using Recetas.Dominio.Puertos;

namespace Recetas.Aplicacion.Moderacion;

public enum ResultadoDeRetirada
{
    Correcto,

    /// <summary>
    /// No existe, es privada, o es del propio responsable. Se responden igual por
    /// la misma razón que en el resto de la API: distinguirlas diría qué recetas
    /// existen.
    /// </summary>
    NoEncontrada
}

/// <summary>
/// El responsable del servicio devuelve a privada una receta pública ajena, y se
/// avisa a su autor.
/// </summary>
/// <remarks>
/// <b>Caso de uso propio y no una bandera dentro de la gestión del recetario.</b>
/// La 015 lo resolvió con un parámetro <c>esResponsable</c> en
/// <c>CambiarVisibilidadAsync</c>, y funcionaba mientras la operación era
/// idéntica a que el autor dejara de compartir. Dejó de serlo en cuanto una
/// avisa por correo y la otra no.
/// <para>
/// Separarlas devuelve a <c>GestionDeRecetas</c> a saber solo de autoría, y quita
/// de en medio un parámetro que podía colarse por descuido en editar o borrar.
/// Es la misma razón por la que el dominio distingue <c>EsDe</c> de
/// <c>PuedeVerla</c>: cuando dos cosas dejan de significar lo mismo, compartir el
/// camino es lo que produce los errores.
/// </para>
/// </remarks>
public sealed class RetirarPorModeracion(
    IRepositorioDeRecetas recetas,
    IRepositorioDeUsuarios usuarios,
    IEnviadorDeCorreo enviadorDeCorreo,
    IReloj reloj,
    ILogger<RetirarPorModeracion> registro)
{
    public async Task<ResultadoDeRetirada> EjecutarAsync(
        Guid responsableId,
        Guid recetaId,
        CancellationToken cancelacion = default)
    {
        var receta = await recetas.BuscarPorIdAsync(recetaId, cancelacion);

        // Solo lo ajeno y ya publicado. Sobre lo propio ya están las acciones de
        // autor, y sobre lo privado no hay nada que retirar: nadie ha podido
        // verlo para denunciarlo.
        if (receta is null || receta.EsDe(responsableId) || !receta.EsPublica)
        {
            return ResultadoDeRetirada.NoEncontrada;
        }

        receta.Despublicar(reloj.Ahora);

        await recetas.GuardarCambiosAsync(cancelacion);

        await AvisarAlAutorAsync(receta.AutorId, receta.Nombre, cancelacion);

        return ResultadoDeRetirada.Correcto;
    }

    /// <summary>
    /// El aviso va <b>después</b> de retirar y su fallo no la deshace: la receta
    /// ya está fuera, y devolver un error haría pensar al responsable que no ha
    /// surtido efecto.
    /// </summary>
    private async Task AvisarAlAutorAsync(
        Guid autorId,
        string nombreDeLaReceta,
        CancellationToken cancelacion)
    {
        try
        {
            var autor = await usuarios.BuscarPorIdAsync(autorId, cancelacion);

            if (autor is null)
            {
                return;
            }

            await enviadorDeCorreo.EnviarAvisoDeRetiradaAsync(
                autor.Correo,
                nombreDeLaReceta,
                cancelacion);
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            registro.LogError(
                excepcion,
                "Receta retirada, pero no se pudo avisar a su autor {Autor}.",
                autorId);
        }
    }
}
