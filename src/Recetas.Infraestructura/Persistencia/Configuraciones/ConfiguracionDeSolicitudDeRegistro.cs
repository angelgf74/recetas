using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recetas.Dominio.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDeSolicitudDeRegistro : IEntityTypeConfiguration<SolicitudDeRegistro>
{
    public void Configure(EntityTypeBuilder<SolicitudDeRegistro> constructor)
    {
        constructor.ToTable("solicitudes_de_registro");

        constructor.HasKey(solicitud => solicitud.Id);

        constructor.Property(solicitud => solicitud.Correo)
            .HasConversion(
                correo => correo.Valor,
                valor => CorreoElectronico.Crear(valor))
            .HasColumnName("correo")
            .HasMaxLength(CorreoElectronico.LongitudMaxima)
            .IsRequired();

        // Sin índice único: el mismo correo puede acumular solicitudes a lo largo
        // del tiempo, y las anteriores se invalidan en vez de borrarse.
        constructor.HasIndex(solicitud => solicitud.Correo)
            .HasDatabaseName("ix_solicitudes_correo");

        constructor.Property(solicitud => solicitud.HashDelToken)
            .HasColumnName("hash_del_token")
            .HasMaxLength(64)
            .IsRequired();

        // Se busca por este campo en cada intento de completar el alta.
        constructor.HasIndex(solicitud => solicitud.HashDelToken)
            .IsUnique()
            .HasDatabaseName("ix_solicitudes_hash_del_token");

        constructor.Property(solicitud => solicitud.FechaDeCreacion)
            .HasColumnName("fecha_de_creacion")
            .IsRequired();

        constructor.Property(solicitud => solicitud.FechaDeCaducidad)
            .HasColumnName("fecha_de_caducidad")
            .IsRequired();

        constructor.Property(solicitud => solicitud.FechaDeConsumo)
            .HasColumnName("fecha_de_consumo");
    }
}
