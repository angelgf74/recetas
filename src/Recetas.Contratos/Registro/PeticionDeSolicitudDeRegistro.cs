using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Registro;

/// <summary>Paso 1 del alta: solo el correo. La contraseña llega en el paso 2.</summary>
public sealed class PeticionDeSolicitudDeRegistro
{
    [Required(ErrorMessage = "Indica tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ese correo electrónico no parece válido.")]
    [MaxLength(254)]
    public string Correo { get; set; } = string.Empty;
}
