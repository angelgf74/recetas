namespace Recetas.Infraestructura.Correo;

/// <summary>
/// Textos de los correos, en un solo sitio para que los dos enviadores
/// (Brevo y consola) manden exactamente lo mismo.
/// </summary>
/// <remarks>
/// Cada mensaje tiene versión HTML <b>y</b> texto plano. No es un adorno para
/// clientes antiguos: un correo solo-HTML es una de las señales de spam más
/// habituales, porque el correo legítimo casi siempre incluye ambas partes.
/// Los filtros además comparan las dos versiones, y que digan lo mismo suma.
/// </remarks>
internal static class MensajesDeCorreo
{
    public const string AsuntoDeAlta = "Completa tu alta en Recetas";

    public const string AsuntoDeCuentaExistente = "Alguien ha pedido crear una cuenta con tu correo";

    public static string CuerpoDeAlta(string enlace) =>
        $"""
         <p>Hola:</p>
         <p>Para terminar de crear tu cuenta en Recetas, elige tu contraseña aquí:</p>
         <p><a href="{enlace}">Elegir mi contraseña</a></p>
         <p>Si el botón no funciona, copia esta dirección en tu navegador:<br>
         <span>{enlace}</span></p>
         <p>El enlace caduca en 24 horas y solo se puede usar una vez.</p>
         <p>Si no has pedido tú esta alta, ignora este mensaje: sin abrir el enlace no se crea ninguna cuenta.</p>
         """;

    public static string TextoDeAlta(string enlace) =>
        $"""
         Hola:

         Para terminar de crear tu cuenta en Recetas, elige tu contraseña en esta dirección:

         {enlace}

         El enlace caduca en 24 horas y solo se puede usar una vez.

         Si no has pedido tú esta alta, ignora este mensaje: sin abrir el enlace no se
         crea ninguna cuenta.
         """;

    public static string CuerpoDeCuentaExistente() =>
        """
        <p>Hola:</p>
        <p>Alguien ha intentado crear una cuenta en Recetas con esta dirección, que ya tiene una.</p>
        <p>No hemos hecho ningún cambio. Si has sido tú, inicia sesión con tu contraseña habitual.</p>
        <p>Si no reconoces el intento, puedes ignorar este mensaje.</p>
        """;

    public static string TextoDeCuentaExistente() =>
        """
        Hola:

        Alguien ha intentado crear una cuenta en Recetas con esta dirección, que ya tiene una.

        No hemos hecho ningún cambio. Si has sido tú, inicia sesión con tu contraseña habitual.

        Si no reconoces el intento, puedes ignorar este mensaje.
        """;
}
