namespace Recetas.Dominio.Moderacion;

/// <summary>
/// Aviso de un usuario sobre una receta pública ajena que considera inapropiada.
/// </summary>
/// <remarks>
/// Se guarda además de enviarse por correo. El correo es el aviso; la fila es la
/// constancia de que la denuncia existió. Un buzón lleno, Brevo caído o un filtro
/// de spam no pueden significar que nadie denunció nada.
/// </remarks>
public sealed class Denuncia
{
    /// <summary>
    /// Tope del comentario. Suficiente para explicar qué pasa y corto como para
    /// que nadie use el endpoint para almacenar texto ajeno al aviso.
    /// </summary>
    public const int LongitudMaximaDelComentario = 1_000;

    private Denuncia(
        Guid id,
        Guid recetaId,
        Guid denuncianteId,
        MotivoDeDenuncia motivo,
        string? comentario,
        DateTimeOffset fechaDeCreacion)
    {
        Id = id;
        RecetaId = recetaId;
        DenuncianteId = denuncianteId;
        Motivo = motivo;
        Comentario = comentario;
        FechaDeCreacion = fechaDeCreacion;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Denuncia()
    {
    }

    public Guid Id { get; private set; }

    public Guid RecetaId { get; private set; }

    /// <summary>
    /// Quién denuncia. Se guarda para poder impedir la denuncia repetida y para
    /// distinguir un aviso aislado de una campaña; <b>no se le enseña al autor</b>.
    /// </summary>
    public Guid DenuncianteId { get; private set; }

    public MotivoDeDenuncia Motivo { get; private set; }

    /// <summary>Explicación opcional. Es lo que hace útil el motivo <c>Otro</c>.</summary>
    public string? Comentario { get; private set; }

    public DateTimeOffset FechaDeCreacion { get; private set; }

    public static Denuncia Crear(
        Guid recetaId,
        Guid denuncianteId,
        MotivoDeDenuncia motivo,
        string? comentario,
        DateTimeOffset ahora)
    {
        if (recetaId == Guid.Empty)
        {
            throw new ArgumentException("La denuncia necesita una receta.", nameof(recetaId));
        }

        if (denuncianteId == Guid.Empty)
        {
            throw new ArgumentException("La denuncia necesita un denunciante.", nameof(denuncianteId));
        }

        if (!Enum.IsDefined(motivo))
        {
            throw new ArgumentOutOfRangeException(nameof(motivo), motivo, "Motivo de denuncia desconocido.");
        }

        var limpio = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();

        if (limpio is not null && limpio.Length > LongitudMaximaDelComentario)
        {
            throw new ArgumentException(
                $"El comentario no puede pasar de {LongitudMaximaDelComentario} caracteres.",
                nameof(comentario));
        }

        return new Denuncia(Guid.NewGuid(), recetaId, denuncianteId, motivo, limpio, ahora);
    }
}
