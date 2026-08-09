using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeEtiqueta : IEntityTypeConfiguration<Etiqueta>
{
    public void Configure(EntityTypeBuilder<Etiqueta> constructor)
    {
        constructor.ToTable("etiquetas");

        constructor.HasKey(etiqueta => etiqueta.Id);

        constructor.Property(etiqueta => etiqueta.Nombre)
            .HasConversion(
                nombre => nombre.Valor,
                valor => NombreDeEtiqueta.Crear(valor))
            .HasColumnName("nombre")
            .HasMaxLength(NombreDeEtiqueta.LongitudMaxima)
            .IsRequired();

        // Garantía real de que el catálogo no se duplica; la comprobación previa
        // en la aplicación cubre el caso normal, esta cubre la carrera entre dos
        // peticiones simultáneas que den de alta la misma etiqueta nueva.
        constructor.HasIndex(etiqueta => etiqueta.Nombre)
            .IsUnique()
            .HasDatabaseName("ix_etiquetas_nombre");

        constructor.Property(etiqueta => etiqueta.NombreParaBusqueda)
            .HasColumnName("nombre_para_busqueda")
            .HasMaxLength(NombreDeEtiqueta.LongitudMaxima)
            .IsRequired();

        constructor.HasIndex(etiqueta => etiqueta.NombreParaBusqueda)
            .HasDatabaseName("ix_etiquetas_nombre_para_busqueda");
    }
}

public sealed class ConfiguracionDeEtiquetaDeReceta : IEntityTypeConfiguration<EtiquetaDeReceta>
{
    public void Configure(EntityTypeBuilder<EtiquetaDeReceta> constructor)
    {
        constructor.ToTable("etiquetas_de_receta");

        constructor.HasKey(vinculo => new { vinculo.RecetaId, vinculo.EtiquetaId });

        constructor.Property(vinculo => vinculo.RecetaId).HasColumnName("receta_id");
        constructor.Property(vinculo => vinculo.EtiquetaId).HasColumnName("etiqueta_id");

        // La relación con Receta se configura desde el otro lado, en
        // ConfiguracionDeReceta, igual que con IngredienteDeReceta: es donde vive
        // el resto de las cascadas de la receta.

        constructor.HasOne(vinculo => vinculo.Etiqueta)
            .WithMany()
            .HasForeignKey(vinculo => vinculo.EtiquetaId)
            // Restrict y no Cascade: borrar una receta no debe poder borrar del
            // catálogo compartido una etiqueta que usan otras.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
