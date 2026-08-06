using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Moderacion;

/// <summary>Cuerpo de una denuncia sobre una receta pública ajena.</summary>
public sealed class PeticionDeDenuncia
{
    /// <summary>
    /// Uno de los valores de <c>MotivoDeDenuncia</c>. Como texto y no como número,
    /// igual que el tipo de plato: así el contrato no depende del orden del
    /// enumerado y un cliente antiguo no acaba denunciando por un motivo distinto
    /// del que eligió el usuario.
    /// </summary>
    [Required(ErrorMessage = "Indica por qué la denuncias.")]
    public string Motivo { get; set; } = string.Empty;

    /// <summary>Explicación opcional. Es lo que hace útil el motivo «Otro».</summary>
    [MaxLength(LongitudMaximaDelComentario,
        ErrorMessage = "El comentario no puede pasar de {1} caracteres.")]
    public string? Comentario { get; set; }

    /// <summary>Debe coincidir con <c>Denuncia</c> en el dominio; se repite porque Contratos no lo referencia.</summary>
    public const int LongitudMaximaDelComentario = 1_000;
}
