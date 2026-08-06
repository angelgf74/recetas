using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Moderacion;
using Recetas.Dominio.Recetas;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeDenuncia : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> constructor)
    {
        constructor.ToTable("denuncias");

        constructor.HasKey(denuncia => denuncia.Id);

        constructor.Property(denuncia => denuncia.RecetaId)
            .HasColumnName("receta_id")
            .IsRequired();

        // En cascada: si la receta se borra, la denuncia sobre ella deja de tener
        // objeto. El histórico de moderación no vale de nada sin el contenido.
        constructor.HasOne<Receta>()
            .WithMany()
            .HasForeignKey(denuncia => denuncia.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.Property(denuncia => denuncia.DenuncianteId)
            .HasColumnName("denunciante_id")
            .IsRequired();

        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(denuncia => denuncia.DenuncianteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un usuario, una denuncia por receta. El caso de uso ya lo comprueba
        // antes; esto cubre dos peticiones simultáneas, que la comprobación previa
        // no puede ver.
        constructor.HasIndex(denuncia => new { denuncia.DenuncianteId, denuncia.RecetaId })
            .IsUnique()
            .HasDatabaseName("ix_denuncias_denunciante_receta");

        // Como texto y no como número: el valor guardado sigue siendo legible si
        // algún día se reordena el enumerado.
        constructor.Property(denuncia => denuncia.Motivo)
            .HasConversion<string>()
            .HasColumnName("motivo")
            .HasMaxLength(32)
            .IsRequired();

        constructor.Property(denuncia => denuncia.Comentario)
            .HasColumnName("comentario")
            .HasMaxLength(Denuncia.LongitudMaximaDelComentario);

        constructor.Property(denuncia => denuncia.FechaDeCreacion)
            .HasColumnName("fecha_de_creacion")
            .IsRequired();
    }
}
