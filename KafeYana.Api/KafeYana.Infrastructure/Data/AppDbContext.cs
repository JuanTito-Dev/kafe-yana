using KafeYana.Core.Entities.Entity;
using KafeYana.Core.Entities.Inventario;
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
        }
    }
}
