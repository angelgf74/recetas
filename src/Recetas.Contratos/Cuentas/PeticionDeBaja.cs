using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Cuentas;

/// <summary>
/// Confirmación para borrar la cuenta.
/// </summary>
/// <remarks>
/// Va en el cuerpo de un <c>DELETE</c>, que es poco habitual pero correcto: en la
/// URL, la contraseña acabaría en los registros del servidor y en el historial del
/// navegador.
/// </remarks>
public sealed class PeticionDeBaja
{
    [Required(ErrorMessage = "Escribe tu contraseña para confirmar.")]
    public string Contrasena { get; set; } = string.Empty;
}
