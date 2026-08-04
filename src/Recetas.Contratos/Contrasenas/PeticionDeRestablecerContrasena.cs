using System.ComponentModel.DataAnnotations;
using Recetas.Contratos.Registro;

namespace Recetas.Contratos.Contrasenas;

/// <summary>
/// Paso 2 del restablecimiento. La contraseña viaja aquí, en el cuerpo de un POST,
/// nunca en la URL ni en el enlace del correo.
/// </summary>
public sealed class PeticionDeRestablecerContrasena
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige una contraseña.")]
    [MinLength(PeticionDeCompletarRegistro.LongitudMinimaDeContrasena,
        ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
    [MaxLength(PeticionDeCompletarRegistro.LongitudMaximaDeContrasena)]
    public string Contrasena { get; set; } = string.Empty;
}
