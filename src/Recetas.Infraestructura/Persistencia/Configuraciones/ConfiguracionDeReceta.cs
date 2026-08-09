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

        // Columna de apoyo para la búsqueda: minúsculas y sin acentos. Existe
        // porque la extensión `unaccent` de PostgreSQL exigiría privilegios que
        // el despliegue no tiene.
        constructor.Property(receta => receta.NombreParaBusqueda)
            .HasColumnName("nombre_para_busqueda")
            .HasMaxLength(Receta.LongitudMaximaDelNombre)
            .IsRequired();

        constructor.HasIndex(receta => receta.NombreParaBusqueda)
            .HasDatabaseName("ix_recetas_nombre_para_busqueda");

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

        // Nula: las recetas anteriores a la feature 010 no dicen para cuántos son,
        // y un valor por defecto sería un dato inventado del que depende el escalado.
        constructor.Property(receta => receta.Raciones)
            .HasColumnName("raciones");

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

        constructor.Metadata
            .FindNavigation(nameof(Receta.Etiquetas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Cascada hacia el vínculo, nunca hacia el catálogo: borrar la receta
        // borra sus filas de etiquetas_de_receta, no las etiquetas compartidas.
        constructor.HasMany(receta => receta.Etiquetas)
            .WithOne()
            .HasForeignKey(vinculo => vinculo.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.Property(receta => receta.FotoDePortadaElegidaId)
            .HasColumnName("foto_de_portada_elegida_id");

        // Referencia cruzada a propósito: recetas -> fotos aquí, fotos -> recetas
        // más arriba. SetNull y no Cascade: borrar la foto elegida no debe borrar
        // la receta, solo dejar de tener una elección explícita. El dominio ya
        // limpia esta misma columna en la misma transacción (`QuitarFoto`); la FK
        // es la red por si algún día algo borra una fila de `fotos` sin pasar
        // por ahí.
        constructor.HasOne<Foto>()
            .WithMany()
            .HasForeignKey(receta => receta.FotoDePortadaElegidaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
