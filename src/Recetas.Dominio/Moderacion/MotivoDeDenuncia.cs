namespace Recetas.Dominio.Moderacion;

/// <summary>
/// Por qué se denuncia una receta.
/// </summary>
/// <remarks>
/// Lista cerrada y corta a propósito. Un desplegable con veinte motivos hace que
/// nadie lea y todo el mundo elija el primero; estos son los que de verdad
/// distinguen qué hacer con el aviso.
/// </remarks>
public enum MotivoDeDenuncia
{
    /// <summary>No es una receta: texto de relleno, publicidad disfrazada, pruebas.</summary>
    NoEsUnaReceta,

    /// <summary>Insultos, odio o acoso.</summary>
    Ofensivo,

    /// <summary>Contenido sexual.</summary>
    Sexual,

    /// <summary>Contenido violento.</summary>
    Violento,

    /// <summary>Spam o enlaces comerciales.</summary>
    Spam,

    /// <summary>Copia de contenido ajeno sin permiso.</summary>
    Derechos,

    /// <summary>Cualquier otra cosa. Es el motivo que hace útil el comentario.</summary>
    Otro
}
