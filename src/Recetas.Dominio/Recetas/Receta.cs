namespace Recetas.Dominio.Recetas;

/// <summary>
/// Una receta. Pertenece siempre a un usuario y nace privada.
/// </summary>
public sealed class Receta
{
    public const int LongitudMaximaDelNombre = 120;
    public const int LongitudMaximaDeLaElaboracion = 20_000;

    private readonly List<IngredienteDeReceta> _ingredientes = [];
    private readonly List<Foto> _fotos = [];

    private Receta(
        Guid id,
        Guid autorId,
        string nombre,
        TipoDePlato tipoDePlato,
        string elaboracion,
        DateTimeOffset ahora)
    {
        Id = id;
        AutorId = autorId;
        Nombre = nombre;
        TipoDePlato = tipoDePlato;
        Elaboracion = elaboracion;
        Visibilidad = Visibilidad.Privada;
        FechaDeCreacion = ahora;
        FechaDeModificacion = ahora;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Receta()
    {
        Nombre = null!;
        Elaboracion = null!;
    }

    public Guid Id { get; private set; }

    /// <summary>Identificador del <c>Usuario</c> autor. Nunca cambia.</summary>
    public Guid AutorId { get; private set; }

    public string Nombre { get; private set; }

    public TipoDePlato TipoDePlato { get; private set; }

    /// <summary>
    /// Los pasos, como texto. No se modela como lista porque partir en pasos es
    /// una decisión de presentación: la web puede hacerlo por saltos de línea.
    /// </summary>
    public string Elaboracion { get; private set; }

    /// <summary>
    /// Quién puede leerla. Solo la cambia la feature 005: aquí no hay ninguna
    /// operación que la toque, ni siquiera al actualizar.
    /// </summary>
    public Visibilidad Visibilidad { get; private set; }

    public DateTimeOffset FechaDeCreacion { get; private set; }

    public DateTimeOffset FechaDeModificacion { get; private set; }

    public IReadOnlyCollection<IngredienteDeReceta> Ingredientes => _ingredientes;

    /// <summary>
    /// Fotos de la receta. No tienen permisos propios: heredan los de la receta,
    /// y por eso viven dentro de ella y no como entidad suelta.
    /// </summary>
    public IReadOnlyCollection<Foto> Fotos => _fotos;

    public static Receta Crear(
        Guid autorId,
        string nombre,
        TipoDePlato tipoDePlato,
        string elaboracion,
        DateTimeOffset ahora)
    {
        if (autorId == Guid.Empty)
        {
            throw new ArgumentException("Una receta necesita autor.", nameof(autorId));
        }

        return new Receta(
            Guid.NewGuid(),
            autorId,
            ValidarNombre(nombre),
            tipoDePlato,
            ValidarElaboracion(elaboracion),
            ahora);
    }

    /// <summary>
    /// Comprobación de autoría, en el dominio y no en el endpoint.
    /// </summary>
    /// <remarks>
    /// Puesta aquí, es imposible añadir una operación nueva y olvidarse de ella:
    /// los casos de uso tienen que preguntar antes de devolver o modificar nada.
    /// </remarks>
    public bool EsDe(Guid usuarioId) => AutorId == usuarioId;

    /// <summary>
    /// Si ese usuario puede <b>leer</b> la receta: porque es suya, o porque está
    /// publicada.
    /// </summary>
    /// <remarks>
    /// Deliberadamente distinta de <see cref="EsDe"/>, y con un nombre que no se
    /// le parece. Editar, borrar, publicar y gestionar las fotos siguen exigiendo
    /// autoría; usar esta comprobación donde tocaba la otra permitiría a cualquiera
    /// modificar las recetas públicas de los demás.
    /// <para>
    /// "Pública" nunca significa anónima: quien pregunta ya está autenticado. No
    /// hay lectura sin cuenta en ningún caso.
    /// </para>
    /// </remarks>
    public bool PuedeVerla(Guid usuarioId) => EsDe(usuarioId) || Visibilidad == Visibilidad.Publica;

    /// <summary>
    /// Hace la receta visible para el resto de usuarios registrados.
    /// Idempotente: publicar algo ya público no es un error.
    /// </summary>
    public void Publicar(DateTimeOffset ahora)
    {
        if (Visibilidad == Visibilidad.Publica)
        {
            return;
        }

        Visibilidad = Visibilidad.Publica;
        FechaDeModificacion = ahora;
    }

    /// <summary>
    /// La devuelve a privada. Idempotente, por el mismo motivo que
    /// <see cref="Publicar"/>: el usuario pide un estado, no una transición.
    /// </summary>
    public void Despublicar(DateTimeOffset ahora)
    {
        if (Visibilidad == Visibilidad.Privada)
        {
            return;
        }

        Visibilidad = Visibilidad.Privada;
        FechaDeModificacion = ahora;
    }

    public void Actualizar(string nombre, TipoDePlato tipoDePlato, string elaboracion, DateTimeOffset ahora)
    {
        // Ni el autor ni la visibilidad se tocan: no son datos que la edición
        // pueda cambiar, y por eso ni siquiera aparecen como parámetros.
        Nombre = ValidarNombre(nombre);
        TipoDePlato = tipoDePlato;
        Elaboracion = ValidarElaboracion(elaboracion);
        FechaDeModificacion = ahora;
    }

    /// <summary>
    /// Sustituye por completo la lista de ingredientes. Los que ya no aparecen
    /// dejan de estar asociados a la receta.
    /// </summary>
    public void ReemplazarIngredientes(IEnumerable<(Guid IngredienteId, decimal? Cantidad, Unidad Unidad)> lineas)
    {
        ArgumentNullException.ThrowIfNull(lineas);

        var nuevas = lineas
            .Select(linea => IngredienteDeReceta.Crear(Id, linea.IngredienteId, linea.Cantidad, linea.Unidad))
            .ToList();

        if (nuevas.Count == 0)
        {
            throw new ArgumentException("Una receta necesita al menos un ingrediente.", nameof(lineas));
        }

        // El mismo ingrediente dos veces en una receta no tiene significado y
        // además rompería la clave compuesta al guardar.
        if (nuevas.Select(linea => linea.IngredienteId).Distinct().Count() != nuevas.Count)
        {
            throw new ArgumentException("La receta repite algún ingrediente.", nameof(lineas));
        }

        _ingredientes.Clear();
        _ingredientes.AddRange(nuevas);
    }

    public Foto AnadirFoto(TipoDeImagen tipo, long tamanoEnBytes, DateTimeOffset ahora)
    {
        var foto = Foto.Crear(Id, tipo, tamanoEnBytes, ahora);
        _fotos.Add(foto);
        FechaDeModificacion = ahora;

        return foto;
    }

    /// <summary>
    /// Quita una foto de la receta. Devuelve la foto quitada, o <c>null</c> si no
    /// pertenecía a esta receta: quien llama necesita conocer su tipo para poder
    /// borrar también el archivo del almacén.
    /// </summary>
    public Foto? QuitarFoto(Guid fotoId, DateTimeOffset ahora)
    {
        var foto = _fotos.FirstOrDefault(f => f.Id == fotoId);

        if (foto is null)
        {
            return null;
        }

        _fotos.Remove(foto);
        FechaDeModificacion = ahora;

        return foto;
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("La receta necesita un nombre.", nameof(nombre));
        }

        var recortado = nombre.Trim();

        return recortado.Length <= LongitudMaximaDelNombre
            ? recortado
            : throw new ArgumentException(
                $"El nombre no puede pasar de {LongitudMaximaDelNombre} caracteres.", nameof(nombre));
    }

    private static string ValidarElaboracion(string elaboracion)
    {
        if (string.IsNullOrWhiteSpace(elaboracion))
        {
            throw new ArgumentException("La receta necesita una elaboración.", nameof(elaboracion));
        }

        var recortada = elaboracion.Trim();

        return recortada.Length <= LongitudMaximaDeLaElaboracion
            ? recortada
            : throw new ArgumentException(
                $"La elaboración no puede pasar de {LongitudMaximaDeLaElaboracion} caracteres.", nameof(elaboracion));
    }
}
