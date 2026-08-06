using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Recetas.Api.Endpoints;

/// <summary>
/// Extrae el identificador del usuario del token, en un único sitio.
/// </summary>
/// <remarks>
/// Repetir esta lectura en cada endpoint es la forma más fácil de que uno de ellos
/// acabe confiando en un identificador que llega en el cuerpo de la petición, y
/// con él en la posibilidad de operar sobre recetas ajenas.
/// </remarks>
public static class UsuarioAutenticado
{
    public static bool TryObtenerId(this ClaimsPrincipal usuario, out Guid id)
    {
        var valor = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(valor, out id);
    }

    /// <summary>
    /// Correo del usuario autenticado, tomado del token.
    /// </summary>
    /// <remarks>
    /// Sale del JWT y no de la base de datos: el token lo firma el servidor, así
    /// que su contenido no es manipulable, y comprobar quién modera no tiene por
    /// qué costar una consulta.
    /// </remarks>
    public static string? ObtenerCorreo(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? usuario.FindFirstValue(ClaimTypes.Email);
}
