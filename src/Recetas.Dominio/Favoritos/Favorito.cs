namespace Recetas.Dominio.Favoritos;

/// <summary>
/// Marca privada de un usuario sobre una receta que quiere volver a encontrar.
/// </summary>
/// <remarks>
/// <b>No tiene identificador propio: la clave es el par.</b> Una clave sustituta
/// permitiría dos filas iguales con identificadores distintos, y "marcado dos
/// veces" no significa nada.
/// <para>
/// La marca <b>no dice que la receta se pueda ver ahora</b>, solo que se podía
/// cuando se marcó. Quién sale en la lista lo decide la consulta que la lee,
/// contra la visibilidad del momento: así, si el autor despublica, su receta
/// desaparece de los favoritos ajenos sin que nadie tenga que ir a borrar filas.
/// </para>
/// </remarks>
public sealed class Favorito
{
    private Favorito(Guid usuarioId, Guid recetaId, DateTimeOffset fechaDeMarca)
    {
        UsuarioId = usuarioId;
        RecetaId = recetaId;
        FechaDeMarca = fechaDeMarca;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Favorito()
    {
    }

    /// <summary>Quién marca. Es la mitad de la clave, y nunca se enseña al autor.</summary>
    public Guid UsuarioId { get; private set; }

    public Guid RecetaId { get; private set; }

    /// <summary>
    /// Cuándo se marcó. Ordena la lista —lo último marcado arriba— y es el único
    /// dato que la marca aporta más allá de existir.
    /// </summary>
    public DateTimeOffset FechaDeMarca { get; private set; }

    public static Favorito Crear(Guid usuarioId, Guid recetaId, DateTimeOffset ahora)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El favorito necesita un usuario.", nameof(usuarioId));
        }

        if (recetaId == Guid.Empty)
        {
            throw new ArgumentException("El favorito necesita una receta.", nameof(recetaId));
        }

        return new Favorito(usuarioId, recetaId, ahora);
    }
}
