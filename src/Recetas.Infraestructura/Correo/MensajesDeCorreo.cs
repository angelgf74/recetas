using System.Net;

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

    public const string AsuntoDeContrasena = "Recupera tu contraseña de Recetas";

    public static string CuerpoDeContrasena(string enlace) =>
        $"""
         <p>Hola:</p>
         <p>Has pedido volver a entrar en Recetas. Elige aquí tu contraseña nueva:</p>
         <p><a href="{enlace}">Elegir contraseña nueva</a></p>
         <p>Si el botón no funciona, copia esta dirección en tu navegador:<br>
         <span>{enlace}</span></p>
         <p>El enlace caduca en una hora y solo se puede usar una vez.</p>
         <p>Si no lo has pedido tú, ignora este mensaje: tu contraseña actual sigue funcionando y no hemos cambiado nada.</p>
         """;

    public static string TextoDeContrasena(string enlace) =>
        $"""
         Hola:

         Has pedido volver a entrar en Recetas. Elige tu contraseña nueva en esta dirección:

         {enlace}

         El enlace caduca en una hora y solo se puede usar una vez.

         Si no lo has pedido tú, ignora este mensaje: tu contraseña actual sigue
         funcionando y no hemos cambiado nada.
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

    public const string AsuntoDeBaja = "Tu cuenta de Recetas se ha borrado";

    public static string CuerpoDeBaja() =>
        """
        <p>Hola:</p>
        <p>Tu cuenta de Recetas se ha borrado, junto con tus recetas y tus fotos. No queda nada nuestro con tus datos.</p>
        <p>Si algún día quieres volver, puedes crear una cuenta nueva con esta misma dirección.</p>
        <p><strong>Si no has sido tú</strong>, escríbenos respondiendo a este mensaje: para borrarla hacía falta tu contraseña, así que conviene que la cambies en cualquier otro sitio donde uses esa misma.</p>
        """;

    public static string TextoDeBaja() =>
        """
        Hola:

        Tu cuenta de Recetas se ha borrado, junto con tus recetas y tus fotos. No
        queda nada nuestro con tus datos.

        Si algun dia quieres volver, puedes crear una cuenta nueva con esta misma
        direccion.

        Si no has sido tu, escribenos respondiendo a este mensaje: para borrarla
        hacia falta tu contrasena, asi que conviene que la cambies en cualquier
        otro sitio donde uses esa misma.
        """;

    public const string AsuntoDeRetirada = "Tu receta ha dejado de estar compartida";

    /// <summary>
    /// Aviso al autor de una receta retirada.
    /// </summary>
    /// <remarks>
    /// El tono no es casual: dice qué ha pasado, que no se ha perdido nada y a
    /// quién escribir. Un aviso de moderación que suena a sanción invita a
    /// discutir; uno que suena a fallo del sistema invita a ignorarlo.
    /// <para>
    /// El nombre de la receta lo escribe un usuario, así que va escapado.
    /// </para>
    /// </remarks>
    public static string CuerpoDeRetirada(string nombreDeLaReceta) =>
        $"""
         <p>Hola:</p>
         <p>Tu receta <strong>{WebUtility.HtmlEncode(nombreDeLaReceta)}</strong> ha dejado de estar compartida con el resto de usuarios.</p>
         <p><strong>No se ha borrado nada.</strong> Sigue en tu recetario, ahora como privada: puedes verla, editarla y volver a compartirla cuando quieras.</p>
         <p>Si crees que se trata de un error, respóndenos a este mensaje y lo revisamos.</p>
         """;

    public static string TextoDeRetirada(string nombreDeLaReceta) =>
        $"""
         Hola:

         Tu receta "{nombreDeLaReceta}" ha dejado de estar compartida con el resto
         de usuarios.

         NO SE HA BORRADO NADA. Sigue en tu recetario, ahora como privada: puedes
         verla, editarla y volver a compartirla cuando quieras.

         Si crees que se trata de un error, respondenos a este mensaje y lo
         revisamos.
         """;

    public const string AsuntoDeDenuncia = "Han denunciado una receta pública en Recetas";

    /// <summary>
    /// Aviso al responsable del servicio.
    /// </summary>
    /// <remarks>
    /// <b>El nombre de la receta y el comentario los escribe un usuario</b>, así que
    /// van escapados: sin eso, quien denuncia podría meter etiquetas —o un enlace
    /// disfrazado— en un correo que el responsable abre esperando algo de confianza.
    /// </remarks>
    public static string CuerpoDeDenuncia(Guid recetaId, string nombreDeLaReceta, string motivo, string? comentario)
    {
        var nombre = WebUtility.HtmlEncode(nombreDeLaReceta);
        var texto = comentario is null
            ? "<p><em>Sin comentario.</em></p>"
            : $"<p>Comentario:</p><blockquote>{WebUtility.HtmlEncode(comentario)}</blockquote>";

        return $"""
                <p>Alguien ha denunciado una receta pública.</p>
                <p><strong>Receta:</strong> {nombre}<br>
                <strong>Identificador:</strong> {recetaId}<br>
                <strong>Motivo:</strong> {WebUtility.HtmlEncode(motivo)}</p>
                {texto}
                <p>Para retirarla, entra con la cuenta responsable, abre la receta y despublícala.
                Retirarla la devuelve a privada: su autor la conserva.</p>
                """;
    }

    public static string TextoDeDenuncia(Guid recetaId, string nombreDeLaReceta, string motivo, string? comentario) =>
        $"""
         Alguien ha denunciado una receta pública.

         Receta: {nombreDeLaReceta}
         Identificador: {recetaId}
         Motivo: {motivo}

         Comentario: {comentario ?? "(sin comentario)"}

         Para retirarla, entra con la cuenta responsable, abre la receta y despublicala.
         Retirarla la devuelve a privada: su autor la conserva.
         """;
}
