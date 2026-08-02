using Recetas.Dominio.Puertos;

namespace Recetas.Infraestructura.Tiempo;

/// <summary>Reloj real, siempre en UTC.</summary>
public sealed class RelojDelSistema : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}
