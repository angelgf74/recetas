using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Contrasenas;

/// <summary>Paso 1 del restablecimiento: solo el correo.</summary>
public sealed class PeticionDeSolicitudDeContrasena
{
    [Required(ErrorMessage = "Indica tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ese correo electrónico no parece válido.")]
    [MaxLength(254)]
    public string Correo { get; set; } = string.Empty;
}
