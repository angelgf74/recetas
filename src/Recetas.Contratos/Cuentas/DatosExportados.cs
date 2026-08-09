namespace Recetas.Contratos.Cuentas;

/// <summary>
/// Forma del <c>datos.json</c> que va dentro del paquete de exportación.
/// </summary>
/// <remarks>
/// <b>Tipos propios y no los de los endpoints.</b> Este archivo se lo lleva el
/// usuario y puede abrirlo dentro de años: no debe cambiar de forma porque un
/// día se reordene una respuesta de la API.
/// <para>
/// Los nombres van en castellano, como el resto del dominio, porque lo va a
/// abrir una persona.
/// </para>
/// </remarks>
/// <param name="Correo">Correo de la cuenta.</param>
/// <param name="FechaDeAlta">Cuándo se creó la cuenta.</param>
/// <param name="FechaDeExportacion">Cuándo se generó este paquete.</param>
/// <param name="Recetas">Todas sus recetas.</param>
public sealed record DatosExportados(
    string Correo,
    DateTimeOffset FechaDeAlta,
    DateTimeOffset FechaDeExportacion,
    IReadOnlyList<RecetaExportada> Recetas);

/// <param name="Nombre">Nombre de la receta.</param>
/// <param name="TipoDePlato">Momento del menú al que pertenece.</param>
/// <param name="Raciones">Para cuántas raciones son las cantidades, si se indicó.</param>
/// <param name="Visibilidad">
/// <c>Privada</c> o <c>Publica</c>. Se incluye para que el usuario sepa qué había
/// compartido, no solo qué tenía.
/// </param>
/// <param name="Ingredientes">Líneas de ingrediente con su cantidad y unidad.</param>
/// <param name="Elaboracion">Pasos, tal como los escribió.</param>
/// <param name="Fotos">Nombres de los archivos de foto dentro de este mismo paquete.</param>
public sealed record RecetaExportada(
    string Nombre,
    string TipoDePlato,
    int? Raciones,
    string Visibilidad,
    IReadOnlyList<IngredienteExportado> Ingredientes,
    string Elaboracion,
    DateTimeOffset FechaDeCreacion,
    DateTimeOffset FechaDeModificacion,
    IReadOnlyList<string> Fotos);

/// <param name="Nombre">Nombre del ingrediente.</param>
/// <param name="Cantidad">Cantidad, o <c>null</c> si es al gusto.</param>
/// <param name="Unidad">Unidad de medida.</param>
public sealed record IngredienteExportado(string Nombre, decimal? Cantidad, string Unidad);
