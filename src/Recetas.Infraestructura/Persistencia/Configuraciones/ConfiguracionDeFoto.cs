using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Recetas;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeFoto : IEntityTypeConfiguration<Foto>
{
    public void Configure(EntityTypeBuilder<Foto> constructor)
    {
        constructor.ToTable("fotos");

        constructor.HasKey(foto => foto.Id);

        // El identificador lo genera el dominio, NO la base de datos.
        //
        // Sin esto, EF trata la clave `Guid` como generada al insertar y, al
        // encontrar una foto nueva dentro de una receta ya rastreada, ve que la
        // clave viene rellena y deduce que la fila existe: emite un UPDATE en
        // lugar de un INSERT. El síntoma es un DbUpdateConcurrencyException
        // diciendo que la operación afectó a 0 filas, que no menciona la causa.
        //
        // Al resto de entidades no les pasa porque se añaden con Add() y EF marca
        // el grafo entero como nuevo; la foto es la primera que se cuelga de un
        // padre ya existente.
        constructor.Property(foto => foto.Id).ValueGeneratedNever();

        constructor.Property(foto => foto.RecetaId)
            .HasColumnName("receta_id")
            .IsRequired();

        constructor.HasIndex(foto => foto.RecetaId)
            .HasDatabaseName("ix_fotos_receta");

        // Como texto, igual que el resto de enumerados: reordenarlo en el código
        // no debe reescribir el significado de lo ya guardado.
        constructor.Property(foto => foto.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        constructor.Property(foto => foto.TamanoEnBytes)
            .HasColumnName("tamano_en_bytes")
            .IsRequired();

        constructor.Property(foto => foto.FechaDeSubida)
            .HasColumnName("fecha_de_subida")
            .IsRequired();

        // Aquí NO hay ninguna columna con el binario: los bytes viven en disco y
        // esta tabla solo guarda la referencia. Es un límite duro de la constitución.
    }
}
