namespace Recetas.Dominio.Recetas;

/// <summary>
/// Borrador leído de una página web. <b>No es una receta</b>: no tiene autor, ni
/// identificador, ni se persiste.
/// </summary>
/// <remarks>
/// Es un tipo aparte y no una <see cref="Receta"/> a medio construir porque nunca
/// llega a guardarse tal cual: el usuario lo revisa en el formulario y es su envío
/// el que crea la receta. Reutilizar la entidad invitaría a saltarse ese paso, que
/// es justo lo que mantiene la feature dentro de <c>mission.md</c>.
/// </remarks>
public sealed record RecetaImportada(
    string Nombre,
    string Elaboracion,
    int? Raciones,
    IReadOnlyList<LineaDeIngredienteImportada> Ingredientes);

/// <summary>
/// Una línea de ingrediente tal como se ha podido interpretar del texto de la web.
/// </summary>
/// <param name="Nombre">
/// Nombre del ingrediente. Si el texto no se ha sabido interpretar, es el texto
/// entero: perder un ingrediente es peor que dejarlo sin cantidad.
/// </param>
public sealed record LineaDeIngredienteImportada(string Nombre, decimal? Cantidad, Unidad Unidad);
