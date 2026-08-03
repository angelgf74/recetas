namespace Recetas.Dominio.Recetas;

/// <summary>
/// Quién puede leer una receta.
/// </summary>
/// <remarks>
/// Toda receta nace <see cref="Privada"/>. Pasar a <see cref="Publica"/> es una
/// acción explícita del autor y llega con la feature 005; hasta entonces nada en
/// el sistema puede cambiar este valor.
/// <para>
/// `Publica` significa "visible para cualquier usuario registrado", nunca
/// "abierta a internet": no hay lectura anónima en ningún caso.
/// </para>
/// </remarks>
public enum Visibilidad
{
    Privada,
    Publica
}
