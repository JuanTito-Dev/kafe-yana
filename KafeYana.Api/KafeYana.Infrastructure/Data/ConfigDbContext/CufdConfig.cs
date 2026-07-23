using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CufdConfig : IEntityTypeConfiguration<Cufd>
    {
        public void Configure(EntityTypeBuilder<Cufd> builder)
        {
            builder.ToTable("Cufd");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.CodigoControl)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Direccion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaVigencia)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.CodigoSucursal)
                .IsRequired();

            builder.Property(x => x.CodigoPuntoVenta)
                .IsRequired();

            builder.Property(x => x.FechaRegistro)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            builder.Property(x => x.FechaEmisionSolicitud)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Índice compuesto: el lookup de "CUFD vigente para (sucursal, PV)"
            // (en CufdService.ObtenerCufdVigenteAsync) filtra por estas dos columnas
            // y ordena por FechaRegistro DESC. Sin índice, hace seq scan y degrada
            // cuando la tabla crece con histórico de días previos.
            builder.HasIndex(c => new { c.CodigoSucursal, c.CodigoPuntoVenta })
                .HasDatabaseName("IX_Cufd_SucursalPuntoVenta");
        }
    }
}
