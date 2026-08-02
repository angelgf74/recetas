namespace Recetas.Dominio.Salud;

/// <summary>
/// Resultado de comprobar si el sistema puede atender peticiones.
/// </summary>
public enum EstadoDeSalud
{
    /// <summary>Todas las dependencias responden.</summary>
    Correcto,

    /// <summary>Alguna dependencia imprescindible no responde: el sistema no puede operar.</summary>
    Degradado
}
