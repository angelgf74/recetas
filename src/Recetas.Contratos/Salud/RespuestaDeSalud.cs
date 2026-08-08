namespace Recetas.Contratos.Salud;

/// <summary>
/// Respuesta pública de <c>GET /salud</c>. Parte del contrato de la API:
/// la consumen tanto la web como cualquier cliente futuro.
/// </summary>
/// <param name="Estado">Texto legible del estado global: <c>correcto</c> o <c>degradado</c>.</param>
/// <param name="BaseDeDatos">Si la base de datos responde.</param>
/// <param name="Almacenamiento">
/// Si el disco de las fotos existe, acepta escrituras y tiene espacio. Va aparte
/// del estado global para que quien reciba el aviso sepa **qué** mirar sin tener
/// que entrar en el servidor.
/// </param>
public sealed record RespuestaDeSalud(string Estado, bool BaseDeDatos, bool Almacenamiento = true);
