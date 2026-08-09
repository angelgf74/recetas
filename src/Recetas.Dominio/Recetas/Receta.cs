namespace Recetas.Dominio.Recetas;

/// <summary>
/// Una receta. Pertenece siempre a un usuario y nace privada.
/// </summary>
public sealed class Receta
{
    public const int LongitudMaximaDelNombre = 120;
    public const int LongitudMaximaDeLaElaboracion = 20_000;

    public const int RacionesMinimas = 1;

    /// <summary>Tope alto pero finito: evita factores absurdos al escalar.</summary>
    public const int RacionesMaximas = 100;

    /// <summary>
    /// Tope de etiquetas por receta. Sin él, el campo sería una forma de meter
    /// texto libre sin límite, que es justo lo que ya cubre la elaboración con
    /// su propio tope.
    /// </summary>
    public const int MaximoDeEtiquetas = 10;

    private readonly List<IngredienteDeReceta> _ingredientes = [];
    private readonly List<Foto> _fotos = [];
    private readonly List<EtiquetaDeReceta> _etiquetas = [];

    private Receta(
        Guid id,
        Guid autorId,
        string nombre,
        TipoDePlato tipoDePlato,
        string elaboracion,
        int? raciones,
        DateTimeOffset ahora)
    {
        Id = id;
        AutorId = autorId;
        Nombre = nombre;
        NombreParaBusqueda = TextoParaBusqueda.Normalizar(nombre);
        TipoDePlato = tipoDePlato;
        Elaboracion = elaboracion;
        Raciones = raciones;
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

    /// <summary>
    /// El nombre en minúsculas y sin acentos. Es contra esta columna contra la que
    /// busca la feature 006; <see cref="Nombre"/> conserva la forma que escribió
    /// el usuario y es la que se muestra.
    /// </summary>
    public string NombreParaBusqueda { get; private set; } = string.Empty;

    public TipoDePlato TipoDePlato { get; private set; }

    /// <summary>
    /// Los pasos, como texto. No se modela como lista porque partir en pasos es
    /// una decisión de presentación: la web puede hacerlo por saltos de línea.
    /// </summary>
    public string Elaboracion { get; private set; }

    /// <summary>
    /// Para cuántas raciones son las cantidades guardadas, o <c>null</c> si no se
    /// sabe.
    /// </summary>
    /// <remarks>
    /// Opcional a propósito: las recetas anteriores a la feature 010 no lo dicen, y
    /// ponerles un "4" por defecto sería inventarse un dato del que dependen todos
    /// los cálculos de esta feature. Sin raciones no se puede escalar, y eso es
    /// justo lo que hay que mostrar.
    /// </remarks>
    public int? Raciones { get; private set; }

    /// <summary>
    /// Quién puede leerla. Solo la cambia la feature 005: aquí no hay ninguna
    /// operación que la toque, ni siquiera al actualizar.
    /// </summary>
    public Visibilidad Visibilidad { get; private set; }

    public DateTimeOffset FechaDeCreacion { get; private set; }

    public DateTimeOffset FechaDeModificacion { get; private set; }

    public IReadOnlyCollection<IngredienteDeReceta> Ingredientes => _ingredientes;

    /// <summary>
    /// Etiquetas libres. Complemento de <see cref="TipoDePlato"/>, no sustituto:
    /// puede estar vacía, a diferencia de los ingredientes.
    /// </summary>
    public IReadOnlyCollection<EtiquetaDeReceta> Etiquetas => _etiquetas;

    /// <summary>
    /// Fotos de la receta. No tienen permisos propios: heredan los de la receta,
    /// y por eso viven dentro de ella y no como entidad suelta.
    /// </summary>
    public IReadOnlyCollection<Foto> Fotos => _fotos;

    /// <summary>
    /// Foto que representa a la receta en un listado: la primera que se subió, o
    /// <c>null</c> si no hay ninguna.
    /// </summary>
    /// <remarks>
    /// Derivada, no almacenada. Un campo con el identificador de la portada habría
    /// que mantenerlo al borrar precisamente esa foto, y olvidarlo dejaría los
    /// listados pidiendo una imagen que ya no existe.
    /// <para>
    /// Que el autor pueda elegir otra es otra feature; mientras tanto "la primera
    /// que subiste" es la respuesta menos sorprendente.
    /// </para>
    /// </remarks>
    public Foto? FotoDePortada =>
        _fotos.OrderBy(foto => foto.FechaDeSubida).ThenBy(foto => foto.Id).FirstOrDefault();

    public static Receta Crear(
        Guid autorId,
        string nombre,
        TipoDePlato tipoDePlato,
        string elaboracion,
        DateTimeOffset ahora,
        int? raciones = null)
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
            ValidarRaciones(raciones),
            ahora);
    }

    /// <summary>
    /// Cantidades ajustadas a otro número de raciones.
    /// </summary>
    /// <remarks>
    /// Devuelve una proyección: <b>la receta no se toca</b>. Escalar es una forma
    /// de leerla, no una edición, y por eso ni cambia las líneas ni la fecha de
    /// modificación.
    /// <para>
    /// Se escala <b>siempre desde las cantidades guardadas</b>, nunca desde unas ya
    /// escaladas: encadenar redondeos acumularía error, y pasar de 4 a 6 y luego a
    /// 9 daría un resultado distinto que ir de 4 a 9 directamente.
    /// </para>
    /// </remarks>
    public IReadOnlyList<LineaEscalada> EscalarA(int? raciones)
    {
        var factor = FactorPara(raciones);

        return _ingredientes
            .Select(linea => new LineaEscalada(
                linea.IngredienteId,
                linea.Ingrediente,
                EscaladoDeCantidades.Escalar(linea.Cantidad, linea.Unidad, factor),
                linea.Unidad))
            .ToList();
    }

    /// <summary>
    /// Raciones que representan las cantidades devueltas por <see cref="EscalarA"/>.
    /// Coincide con <see cref="Raciones"/> cuando no hay nada que escalar.
    /// </summary>
    public int? RacionesMostradasPara(int? racionesPedidas) =>
        PuedeEscalarseA(racionesPedidas) ? racionesPedidas : Raciones;

    private bool PuedeEscalarseA(int? racionesPedidas) =>
        Raciones is > 0
        && racionesPedidas is { } pedidas
        && pedidas is >= RacionesMinimas and <= RacionesMaximas;

    /// <summary>
    /// Factor de escalado, o 1 si la receta no dice sus raciones o no se ha pedido
    /// ninguna: sin punto de partida no hay proporción que aplicar.
    /// </summary>
    private decimal FactorPara(int? racionesPedidas) =>
        PuedeEscalarseA(racionesPedidas) ? (decimal)racionesPedidas! / Raciones!.Value : 1m;

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
    /// Si la receta está compartida con el resto de usuarios.
    /// </summary>
    /// <remarks>
    /// No responde a "quién puede hacer qué" —para eso están <see cref="EsDe"/> y
    /// <see cref="PuedeVerla"/>—, solo al estado. Lo usa la moderación, que actúa
    /// sobre lo público y nunca sobre lo privado.
    /// </remarks>
    public bool EsPublica => Visibilidad == Visibilidad.Publica;

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

    public void Actualizar(
        string nombre,
        TipoDePlato tipoDePlato,
        string elaboracion,
        DateTimeOffset ahora,
        int? raciones = null)
    {
        // Ni el autor ni la visibilidad se tocan: no son datos que la edición
        // pueda cambiar, y por eso ni siquiera aparecen como parámetros.
        Nombre = ValidarNombre(nombre);
        // Se recalcula aquí también: si solo se hiciera al crear, renombrar una
        // receta la dejaría encontrable por su nombre antiguo y no por el nuevo.
        NombreParaBusqueda = TextoParaBusqueda.Normalizar(Nombre);
        TipoDePlato = tipoDePlato;
        Elaboracion = ValidarElaboracion(elaboracion);

        // Se admite volver a null: quitar las raciones es una edición legítima
        // ("resulta que no sé para cuántos es").
        Raciones = ValidarRaciones(raciones);
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

    /// <summary>
    /// Sustituye por completo la lista de etiquetas. Una lista vacía es válida:
    /// a diferencia de los ingredientes, las etiquetas no son obligatorias.
    /// </summary>
    public void ReemplazarEtiquetas(IEnumerable<Guid> etiquetaIds)
    {
        ArgumentNullException.ThrowIfNull(etiquetaIds);

        var lista = etiquetaIds.ToList();

        if (lista.Count > MaximoDeEtiquetas)
        {
            throw new ArgumentException(
                $"Una receta no puede llevar más de {MaximoDeEtiquetas} etiquetas.", nameof(etiquetaIds));
        }

        // Mismo criterio que ReemplazarIngredientes: un duplicado no significa
        // nada y además rompería la clave compuesta al guardar. En la práctica
        // no debería llegar hasta aquí —ResolverEtiquetas ya deduplica por
        // nombre— pero el dominio no confía en que quien lo llame lo haga bien.
        if (lista.Distinct().Count() != lista.Count)
        {
            throw new ArgumentException("La receta repite alguna etiqueta.", nameof(etiquetaIds));
        }

        _etiquetas.Clear();
        _etiquetas.AddRange(lista.Select(id => EtiquetaDeReceta.Crear(Id, id)));
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

    private static int? ValidarRaciones(int? raciones) => raciones switch
    {
        null => null,
        >= RacionesMinimas and <= RacionesMaximas => raciones,
        _ => throw new ArgumentOutOfRangeException(
            nameof(raciones),
            $"Las raciones deben estar entre {RacionesMinimas} y {RacionesMaximas}.")
    };

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
