using Microsoft.EntityFrameworkCore;
using Recetas.Dominio.Favoritos;
using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Recetas;
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

    public DbSet<SolicitudDeContrasena> SolicitudesDeContrasena => Set<SolicitudDeContrasena>();

    public DbSet<Receta> Recetas => Set<Receta>();

    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();

    public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();

    public DbSet<Denuncia> Denuncias => Set<Denuncia>();

    public DbSet<Favorito> Favoritos => Set<Favorito>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);

        modelo.ApplyConfigurationsFromAssembly(typeof(RecetasDbContext).Assembly);
    }
}
