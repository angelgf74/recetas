using System.ComponentModel.DataAnnotations;
using Recetas.Contratos.Registro;

namespace Recetas.Contratos.Contrasenas;

/// <summary>
/// Cambiar la contraseña con la sesión iniciada, sabiendo la actual.
/// </summary>
/// <remarks>
/// Las dos contraseñas viajan en el cuerpo de un <c>PUT</c>, nunca en la URL:
/// mismo motivo que <see cref="Recetas.Contratos.Cuentas.PeticionDeBaja"/>.
/// </remarks>
public sealed class PeticionDeCambioDeContrasena
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige una contraseña nueva.")]
    [MinLength(PeticionDeCompletarRegistro.LongitudMinimaDeContrasena,
        ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
    [MaxLength(PeticionDeCompletarRegistro.LongitudMaximaDeContrasena)]
    public string ContrasenaNueva { get; set; } = string.Empty;
}
