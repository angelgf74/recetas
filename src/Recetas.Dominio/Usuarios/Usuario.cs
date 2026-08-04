namespace Recetas.Dominio.Usuarios;

/// <summary>
/// Cuenta de usuario. Solo existe con el alta completa: correo verificado
/// y contraseña puesta. No hay usuarios a medio crear.
/// </summary>
public sealed class Usuario
{
    private Usuario(Guid id, CorreoElectronico correo, string hashDeContrasena, DateTimeOffset fechaDeAlta)
    {
        Id = id;
        Correo = correo;
        HashDeContrasena = hashDeContrasena;
        FechaDeAlta = fechaDeAlta;
    }

    /// <summary>Constructor para EF Core, que materializa sin pasar por la factoría.</summary>
    private Usuario()
    {
        Correo = null!;
        HashDeContrasena = null!;
    }

    public Guid Id { get; private set; }

    public CorreoElectronico Correo { get; private set; }

    /// <summary>Resultado de derivar la contraseña. Nunca la contraseña en claro.</summary>
    public string HashDeContrasena { get; private set; }

    public DateTimeOffset FechaDeAlta { get; private set; }

    /// <summary>
    /// Sustituye la contraseña. Solo lo invoca el restablecimiento de la feature
    /// 008, tras consumir un enlace válido enviado al buzón del usuario.
    /// </summary>
    /// <remarks>
    /// Recibe el hash ya calculado: el dominio no sabe derivar contraseñas, eso
    /// es cosa del puerto <c>IHasheadorDeContrasenas</c>.
    /// <para>
    /// Cambiarla <b>no invalida las sesiones abiertas</b>: un JWT emitido antes
    /// sigue valiendo hasta que caduque. Es consecuencia de haber elegido tokens
    /// sin estado en la 002, y está documentado en la spec de la 008.
    /// </para>
    /// </remarks>
    public void CambiarContrasena(string hashDeContrasena, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDeContrasena);

        HashDeContrasena = hashDeContrasena;
        FechaDeCambioDeContrasena = ahora;
    }

    /// <summary>Cuándo se cambió la contraseña por última vez. <c>null</c> si nunca.</summary>
    public DateTimeOffset? FechaDeCambioDeContrasena { get; private set; }

    public static Usuario Crear(CorreoElectronico correo, string hashDeContrasena, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDeContrasena);

        return new Usuario(Guid.NewGuid(), correo, hashDeContrasena, ahora);
    }
}
