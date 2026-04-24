using KafeYana.Core.Entities.Entity;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data.ConfigDbContext;
using KafeYana.Infrastructure.Data.ConfigDbContext.Indentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Comprado> Comprados { get; set; }

        public DbSet<Promocion> Promociones { get; set; }

        public DbSet<PromocionDetalle> DetallePromciones { get; set; }

        public DbSet<Elaborado> Elaborados { get; set; }

        public DbSet<Receta> Recetas { get; set; }

        public DbSet<Detalle> DetalleReceta { get; set; }

        public DbSet<Insumo> Insumos { get; set; }

        public DbSet<Variacion> Variaciones { get; set; }

        public DbSet<Opcion> Opciones { get; set; }

        public DbSet<Ajuste> Ajustes { get; set; }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Stock_Ajuste> AjustesStock {get; set;}

        public DbSet<Mesa> Mesas { get; set; }

        public DbSet<Venta> Ventas {  get; set; }

        public DbSet<Detalle_venta> Detalle_ventas {  get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<Ronda> Rondas { get; set; }

        public DbSet<Detalle_ronda> Detalle_rondas { get; set; }

        public DbSet<Proveedor> Proveedores { get; set; }

        public DbSet<Detalle_ronda> Detalle_Rondas { get; set; }

        public DbSet<Detalle_Ronda_Opcion> Detalle_Rondas_Opciones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(UsuarioConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(RefreshTokenConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CategoriaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ProductoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CompradoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ElaboradoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(DetalleConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(InsumoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(VariacionConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(OpcionConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(AjusteConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PromocionConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PromocionDetalleConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ClienteConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(Stock_AjusteConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(MesaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(VentaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(Detalle_ventaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PedidoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(RondaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ProveedorConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(Detalle_RondaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(Detalle_Ronda_OpcionConfig).Assembly);
        }
    }
}
