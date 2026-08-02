using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Recetas.Dominio.Puertos;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Seguridad;

/// <summary>Emite JWT firmados con HMAC-SHA256.</summary>
public sealed class EmisorDeAccesoJwt(IOptions<OpcionesDeJwt> opciones, IReloj reloj) : IEmisorDeAcceso
{
    private readonly OpcionesDeJwt _opciones = opciones.Value;

    public AccesoEmitido Emitir(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var ahora = reloj.Ahora;
        var caducidad = ahora.Add(_opciones.Duracion);

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveDeFirma));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = caducidad.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Correo.Valor),
                // Identificador único del token: deja la puerta abierta a una
                // lista de revocación sin tener que cambiar el formato.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccesoEmitido(token, caducidad);
    }
}
