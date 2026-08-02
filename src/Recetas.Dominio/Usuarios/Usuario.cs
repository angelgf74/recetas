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

    public static Usuario Crear(CorreoElectronico correo, string hashDeContrasena, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDeContrasena);

        return new Usuario(Guid.NewGuid(), correo, hashDeContrasena, ahora);
    }
}
