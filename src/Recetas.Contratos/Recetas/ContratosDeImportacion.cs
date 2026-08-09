using System.ComponentModel.DataAnnotations;

namespace Recetas.Contratos.Recetas;

/// <summary>Dirección de la página de la que leer la receta.</summary>
public sealed class PeticionDeImportacion
{
    [Required(ErrorMessage = "Indica la dirección de la receta.")]
    [Url(ErrorMessage = "Eso no parece una dirección web.")]
    [MaxLength(2048)]
    public string Direccion { get; set; } = string.Empty;
}

/// <summary>
/// Borrador leído de una página. <b>No es una receta guardada</b>: el cliente lo
/// pinta en el formulario y es el envío del usuario lo que la crea.
/// </summary>
/// <param name="Origen">
/// Dirección final de la que salió el texto, redirecciones ya seguidas. Se devuelve
/// para que la interfaz pueda decir de dónde viene lo que el usuario está a punto
/// de guardar.
/// </param>
/// <param name="Imagen">
/// La foto de la receta, ya descargada y sin metadatos, o <c>null</c> si la
/// página no declaraba ninguna o no se pudo traer (027). El cliente la guarda
/// en memoria y la sube con <c>POST /recetas/{id}/fotos</c> justo después de
/// crear la receta: el servidor no guarda nada hasta entonces.
/// </param>
public sealed record RespuestaDeImportacion(
    string Nombre,
    string Elaboracion,
    int? Raciones,
    IReadOnlyCollection<LineaDeIngredientePeticion> Ingredientes,
    string Origen,
    byte[]? Imagen = null);
