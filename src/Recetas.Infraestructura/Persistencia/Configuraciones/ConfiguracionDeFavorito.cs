using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Favoritos;
using Recetas.Dominio.Recetas;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeFavorito : IEntityTypeConfiguration<Favorito>
{
    public void Configure(EntityTypeBuilder<Favorito> constructor)
    {
        constructor.ToTable("favoritos");

        // La clave es el par, sin columna sustituta: así "marcado dos veces" no
        // puede existir ni aunque lleguen dos peticiones a la vez, que es lo que
        // la comprobación previa del caso de uso no alcanza a ver.
        constructor.HasKey(favorito => new { favorito.UsuarioId, favorito.RecetaId });

        constructor.Property(favorito => favorito.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        constructor.Property(favorito => favorito.RecetaId)
            .HasColumnName("receta_id")
            .IsRequired();

        // En cascada las dos, igual que en las denuncias: una marca sobre una
        // receta borrada no significa nada, y las marcas de una cuenta borrada se
        // van con ella.
        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(favorito => favorito.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasOne<Receta>()
            .WithMany()
            .HasForeignKey(favorito => favorito.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);

        // La clave primaria ya indexa por usuario, que es como se lee la lista.
        // Este cubre el otro sentido: borrar una receta tiene que encontrar sus
        // marcas sin recorrer la tabla entera.
        constructor.HasIndex(favorito => favorito.RecetaId)
            .HasDatabaseName("ix_favoritos_receta");

        constructor.Property(favorito => favorito.FechaDeMarca)
            .HasColumnName("fecha_de_marca")
            .IsRequired();
    }
}
