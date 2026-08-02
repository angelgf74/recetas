using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia;

/// <summary>
/// Contexto de EF Core contra PostgreSQL.
/// </summary>
public class RecetasDbContext(DbContextOptions<RecetasDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<SolicitudDeRegistro> SolicitudesDeRegistro => Set<SolicitudDeRegistro>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);

        modelo.ApplyConfigurationsFromAssembly(typeof(RecetasDbContext).Assembly);
    }
}
