using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeReceta : IEntityTypeConfiguration<Receta>
{
    public void Configure(EntityTypeBuilder<Receta> constructor)
    {
        constructor.ToTable("recetas");

        constructor.HasKey(receta => receta.Id);

        constructor.Property(receta => receta.AutorId)
            .HasColumnName("autor_id")
            .IsRequired();

        // Todas las consultas del recetario filtran por autor.
        constructor.HasIndex(receta => receta.AutorId)
            .HasDatabaseName("ix_recetas_autor");

        constructor.Property(receta => receta.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(Receta.LongitudMaximaDelNombre)
            .IsRequired();

        // Los enumerados se guardan como texto, no como número: así reordenar el
        // enumerado en el código no reescribe el significado de lo ya guardado.
        constructor.Property(receta => receta.TipoDePlato)
            .HasColumnName("tipo_de_plato")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(receta => receta.Elaboracion)
            .HasColumnName("elaboracion")
            .HasMaxLength(Receta.LongitudMaximaDeLaElaboracion)
            .IsRequired();

        constructor.Property(receta => receta.Visibilidad)
            .HasColumnName("visibilidad")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        constructor.Property(receta => receta.FechaDeCreacion)
            .HasColumnName("fecha_de_creacion")
            .IsRequired();

        constructor.Property(receta => receta.FechaDeModificacion)
            .HasColumnName("fecha_de_modificacion")
            .IsRequired();

        // La colección se expone como IReadOnlyCollection, así que EF necesita
        // que se le indique el campo de respaldo.
        constructor.Metadata
            .FindNavigation(nameof(Receta.Ingredientes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        constructor.HasMany(receta => receta.Ingredientes)
            .WithOne()
            .HasForeignKey(linea => linea.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.Metadata
            .FindNavigation(nameof(Receta.Fotos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Cascada: borrar la receta borra las filas de sus fotos. Los archivos del
        // disco NO caen por aquí; de eso se encarga el caso de uso, que los borra
        // antes de tocar la base de datos.
        constructor.HasMany(receta => receta.Fotos)
            .WithOne()
            .HasForeignKey(foto => foto.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
