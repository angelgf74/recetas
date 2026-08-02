namespace Recetas.Contratos.Sesiones;

/// <param name="Token">JWT que el cliente enviará en la cabecera <c>Authorization</c>.</param>
/// <param name="Caducidad">Cuándo deja de valer. No hay revocación: vale hasta esa fecha.</param>
public sealed record RespuestaDeInicioDeSesion(string Token, DateTimeOffset Caducidad);

/// <param name="Id">Identificador del usuario autenticado.</param>
/// <param name="Correo">Su correo electrónico.</param>
public sealed record RespuestaDeIdentidad(Guid Id, string Correo);

/// <summary>Error uniforme de la API. Un solo mensaje legible, sin detalles que ayuden a sondear.</summary>
/// <param name="Mensaje">Texto para mostrar al usuario.</param>
public sealed record RespuestaDeError(string Mensaje);
