using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ProveedorConfig : IEntityTypeConfiguration<Proveedor>
    {
        public void Configure(EntityTypeBuilder<Proveedor> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Razon_Social)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("RazonSocial");

            builder.Property(x => x.Dni)
                .HasMaxLength(20)
                .IsRequired(false)
                .HasColumnName("DNI");

            // Email es requerido pero puede ser string vacío
            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty)
                .HasConversion(
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v,
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v);

            // Teléfono es requerido pero puede ser string vacío
            builder.Property(x => x.Telefono)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty)
                .HasConversion(
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v,
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v);

            // Celular es requerido pero puede ser string vacío
            builder.Property(x => x.Celular)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty)
                .HasConversion(
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v,
                    v => string.IsNullOrWhiteSpace(v) ? string.Empty : v);

            builder.Property(x => x.Direccion)
                .HasMaxLength(300)
                .IsRequired(false);

            // Índices únicos
            builder.HasIndex(x => x.Razon_Social)
                .IsUnique()
                .HasDatabaseName("ix_proveedores_razon_social")
                .HasFilter("\"RazonSocial\" IS NOT NULL");

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("ix_proveedores_email")
                .HasFilter("\"Email\" != ''");

            builder.HasIndex(x => x.Telefono)
                .IsUnique()
                .HasDatabaseName("ix_proveedores_telefono")
                .HasFilter("\"Telefono\" != ''");

            builder.HasIndex(x => x.Celular)
                .IsUnique()
                .HasDatabaseName("ix_proveedores_celular")
                .HasFilter("\"Celular\" != ''");

            builder.ToTable("Proveedores");
        }
    }
}