namespace Recetas.Dominio.Salud;

/// <summary>
/// Resultado de comprobar si el sistema puede atender peticiones.
/// </summary>
/// <remarks>
/// Lleva el detalle de cada pieza y no un simple "correcto o degradado": un
/// "degradado" a secas obliga a quien recibe el aviso a entrar a mirar qué pasa,
/// que es justo el trabajo que una sonda debería ahorrarle.
/// <para>
/// <see cref="EsCorrecto"/> se <b>deriva</b> de las piezas en lugar de guardarse
/// como un tercer campo. Así no puede existir un estado que diga que todo va bien
/// mientras una de sus partes dice lo contrario.
/// </para>
/// </remarks>
/// <param name="BaseDeDatos">Si la base de datos responde.</param>
/// <param name="Almacenamiento">Si el almacenamiento de fotos puede recibirlas.</param>
public sealed record EstadoDeSalud(bool BaseDeDatos, bool Almacenamiento)
{
    /// <summary>
    /// El sistema opera solo si <b>todas</b> sus dependencias responden. Sin base
    /// de datos no hay recetas; sin almacenamiento, subir una foto falla en el
    /// momento en que el usuario le da a guardar.
    /// </summary>
    public bool EsCorrecto => BaseDeDatos && Almacenamiento;
}
