using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeIngrediente : IEntityTypeConfiguration<Ingrediente>
{
    public void Configure(EntityTypeBuilder<Ingrediente> constructor)
    {
        constructor.ToTable("ingredientes");

        constructor.HasKey(ingrediente => ingrediente.Id);

        constructor.Property(ingrediente => ingrediente.Nombre)
            .HasConversion(
                nombre => nombre.Valor,
                valor => NombreDeIngrediente.Crear(valor))
            .HasColumnName("nombre")
            .HasMaxLength(NombreDeIngrediente.LongitudMaxima)
            .IsRequired();

        // Es la garantía real de que el catálogo no se duplica. La comprobación
        // previa en la aplicación cubre el caso normal; este cubre la carrera
        // entre dos peticiones simultáneas que suban el mismo ingrediente nuevo.
        constructor.HasIndex(ingrediente => ingrediente.Nombre)
            .IsUnique()
            .HasDatabaseName("ix_ingredientes_nombre");

        // Sin acentos, para que "pimenton" encuentre "pimentón". NO es único: dos
        // ingredientes distintos pueden normalizar igual ("anís" y "anis"), y
        // exigir unicidad aquí impediría dar de alta el segundo.
        constructor.Property(ingrediente => ingrediente.NombreParaBusqueda)
            .HasColumnName("nombre_para_busqueda")
            .HasMaxLength(NombreDeIngrediente.LongitudMaxima)
            .IsRequired();

        constructor.HasIndex(ingrediente => ingrediente.NombreParaBusqueda)
            .HasDatabaseName("ix_ingredientes_nombre_para_busqueda");
    }
}

public sealed class ConfiguracionDeIngredienteDeReceta : IEntityTypeConfiguration<IngredienteDeReceta>
{
    public void Configure(EntityTypeBuilder<IngredienteDeReceta> constructor)
    {
        constructor.ToTable("ingredientes_de_receta");

        // Clave compuesta: un ingrediente aparece como mucho una vez por receta.
        constructor.HasKey(linea => new { linea.RecetaId, linea.IngredienteId });

        constructor.Property(linea => linea.RecetaId).HasColumnName("receta_id");
        constructor.Property(linea => linea.IngredienteId).HasColumnName("ingrediente_id");

        constructor.Property(linea => linea.Cantidad)
            .HasColumnName("cantidad")
            .HasPrecision(10, 3);

        constructor.Property(linea => linea.Unidad)
            .HasColumnName("unidad")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        constructor.HasOne(linea => linea.Ingrediente)
            .WithMany()
            .HasForeignKey(linea => linea.IngredienteId)
            // Restrict y no Cascade: borrar un ingrediente del catálogo no debe
            // poder vaciar recetas ajenas por debajo. El catálogo no se borra.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
