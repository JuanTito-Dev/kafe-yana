using KafeYana.Core.Entities.Entity;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.Entities.Facturacion;
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

        public DbSet<VentaPago> VentaPagos { get; set; }

        public DbSet<Detalle_Pago> Detalle_Pagos { get; set; }

        public DbSet<NotaAjuste> NotasAjuste { get; set; }

        public DbSet<NotaAjusteDetalle> NotasAjusteDetalles { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<Ronda> Rondas { get; set; }

        public DbSet<Detalle_ronda> Detalle_rondas { get; set; }

        public DbSet<Proveedor> Proveedores { get; set; }

        public DbSet<Detalle_Ronda_ComboItem> Detalle_Ronda_Combo_Items { get; set; }

        public DbSet<ProductoMovimiento> Movimientos_Producto { get; set; }

        public DbSet<InsumoMovimiento> InsumoMovimientos {  get; set; }

        public DbSet<ParaLlevar> ParaLlevar {  get; set; }

        public DbSet<Caja> Cajas { get; set; }

        public DbSet<CajaMovimiento> CajaMovimientos { get; set; }

        public DbSet<CajaHistorial> CajaHistorial {  get; set; }

        public DbSet<CajaHistorialMovimiento> CajaHistorialMovimientos { get; set; }

        public DbSet<ProductoCanjeable> ProductosCanjeables { get; set; }

        public DbSet<PromocionPermanente> PromocionPermanentes { get; set; }

        public DbSet<PromocionPermanenteProgreso> PromocionPermanenteProgresos { get; set; }

        public DbSet<HistorialPromocionPermanente> HistorialPromocionPermanentes { get; set; }

        public DbSet<PromocionTemporada> PromocionTemporadas { get; set; }

        public DbSet<PromocionTemporadaProductoCanjeable> PromocionTemporadaProductosCanjeables { get; set; }

        public DbSet<HistorialPromocionTemporada> HistorialPromocionTemporadas { get; set; }

        public DbSet<HitoCompra> HitosCompra { get; set; }

        public DbSet<HistorialHitoCompra> HistorialHitoCompras { get; set; }

        public DbSet<ReferidosConfig> ReferidosConfigs { get; set; }

        public DbSet<HistorialReferido> HistorialReferidos { get; set; }

        public DbSet<ReglaBasePuntos> ReglaBasePuntos { get; set; }

        public DbSet<AceleradorPuntos> AceleradorPuntos { get; set; }

        public DbSet<HistorialPuntos> HistorialPuntos { get; set; }

        public DbSet<ConfiguracionQr> ConfiguracionQr { get; set; }

        public DbSet<PedidoInventarioComprometido> PedidoInventarioComprometidos { get; set; }

        public DbSet<PedidoInventarioComprometidoLinea> PedidoInventarioComprometidoLineas { get; set; }

        public DbSet<SubVenta> SubVentas { get; set; }

        public DbSet<SubVentaDetalle> SubVentaDetalles { get; set; }

        public DbSet<Cuis> Cuis { get; set; }

        public DbSet<Cufd> Cufd { get; set; }

        public DbSet<CodigoSiat> CodigosSiat { get; set; }

        public DbSet<CatActividad> CatActividades { get; set; }

        public DbSet<PuntoVentaSiat> PuntosVentaSiat { get; set; }

        public DbSet<CatDocumentoSector> CatDocumentosSector { get; set; }

        public DbSet<CatMotivoAnulacion> CatMotivosAnulacion { get; set; }

        public DbSet<CatActividadDocumentoSector> CatActividadesDocumentosSector { get; set; }

        public DbSet<CatLeyenda> CatLeyendas { get; set; }

        public DbSet<CatEventoSignificativo> CatEventosSignificativos { get; set; }

        public DbSet<CatPaisOrigen> CatPaisesOrigen { get; set; }

        public DbSet<CatTipoDocumentoIdentidad> CatTiposDocumentoIdentidad { get; set; }

        public DbSet<CatTipoEmision> CatTiposEmision { get; set; }

        public DbSet<CatTipoMetodoPago> CatMetodosPago { get; set; }

        public DbSet<CatUnidadMedida> CatUnidadesMedida { get; set; }

        public DbSet<EventoSignificativoSiat> EventosSignificativosSiat { get; set; }

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

            builder.ApplyConfigurationsFromAssembly(typeof(Detalle_PagoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(NotaAjusteConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(NotaAjusteDetalleConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PedidoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(RondaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ProveedorConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(Detalle_RondaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ProductoMovimientoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(InsumoMovimientoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(ParaLlevarConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CajaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CajaMovimientoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CajaHistorialConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CajaHistorialMovimientoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PedidoInventarioComprometidoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CuisConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CufdConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CodigoSiatConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatActividadConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(PuntoVentaSiatConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatDocumentoSectorConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatMotivoAnulacionConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatActividadDocumentoSectorConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatLeyendaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatTipoDocumentoIdentidadConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatTipoEmisionConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatTipoMetodoPagoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CatUnidadMedidaConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(VentaPagoConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(EventoSignificativoSiatConfig).Assembly);
        }
    }
}
