using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Registro;

/// <summary>
/// Paso 2 del alta. La contraseña viaja aquí, en el cuerpo de un POST,
/// nunca en la URL ni en el enlace del correo.
/// </summary>
public sealed class PeticionDeCompletarRegistro
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige una contraseña.")]
    [MinLength(LongitudMinimaDeContrasena,
        ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
    [MaxLength(LongitudMaximaDeContrasena)]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Debe coincidir con <see cref="Recetas.Dominio"/>; se repite aquí porque Contratos no lo referencia.</summary>
    public const int LongitudMinimaDeContrasena = 12;

    public const int LongitudMaximaDeContrasena = 128;
}
