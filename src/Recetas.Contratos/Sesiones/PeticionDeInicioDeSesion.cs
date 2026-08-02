using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Sesiones;

public sealed class PeticionDeInicioDeSesion
{
    [Required(ErrorMessage = "Indica tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ese correo electrónico no parece válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica tu contraseña.")]
    public string Contrasena { get; set; } = string.Empty;
}
