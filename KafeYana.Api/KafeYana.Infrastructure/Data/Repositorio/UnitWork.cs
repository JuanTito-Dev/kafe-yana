using KafeYana.Application.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class UnitWork : IUnitWork
    {
        private readonly AppDbContext _db;
        public IProductoRepositorio productos { get; private set; }

        public IElaboradoRepositorio elaborados { get; private set; }

        public IAjusteStockRepositorio ajustes { get; private set; }

        public IInsumoRepositorio insumos { get; private set; }

        public IRecetaRepositorio recetas { get; private set; }

        public IMesaRepositorio mesas { get; private set; }

        public IPedidoRepositorio Pedidos { get; private set; }

        public IRondaRepositorio rondas { get; private set; }

        public IDetalle_RondaRepositorio detallesRondas { get; private set; }

        public IClienteRespositorio clientes { get; private set; }

        public ICatPaisOrigenRepositorio catPaisesOrigen { get; private set; }

        public IOpcionRepositorio opciones { get; private set; }
        
        public IComboRepositorio Combo { get; private set; }
        
        public IVentaRepositorio ventas { get; private set; }

        public INotaAjusteRepositorio notasAjuste { get; private set; }

        public IVariacionReposiotorio variaciones { get; private set; }
         
        public IProductoMovimientoRepositorio movimientos { get ; private set; }
        public IInsumoMovimientoRepositorio Insumomovientos { get; private set; }

        public IParaLlevarRepositorio parallevar { get; private set; }

        public ICajaRepositorio cajas { get; private set; }

        public ICajaMovimientoRepositorio cajamovimientos { get; private set; }

        public ICajaHistorialRepositorio cajahistorial {  get; private set; }

        public IProveedorRepositorio proveedores { get; private set; }

        public IOrdenCompraRepositorio ordenes {  get; private set; }

        public IProductoCanjeableRepositorio productosCanjeables { get; private set; }

        public IPromocionPermanenteRepositorio promocionPermanentes { get; private set; }

        public IPromocionPermanenteProgresoRepositorio promocionPermanenteProgresos { get; private set; }

        public IHistorialPromocionPermanenteRepositorio historialPromocionPermanentes { get; private set; }

        public IPromocionTemporadaRepositorio promocionTemporadas { get; private set; }

        public IHistorialPromocionTemporadaRepositorio historialPromocionTemporadas { get; private set; }

        public IHitoCompraRepositorio hitosCompra { get; private set; }

        public IHistorialHitoCompraRepositorio historialHitoCompras { get; private set; }

        public IReferidosConfigRepositorio referidosConfig { get; private set; }

        public IHistorialReferidoRepositorio historialReferidos { get; private set; }

        public IReglaBasePuntosRepositorio reglaBasePuntos { get; private set; }

        public IAceleradorPuntosRepositorio aceleradores { get; private set; }

        public IHistorialPuntosRepositorio historialPuntos { get; private set; }

        public IConfiguracionQrRepositorio configuracionQr { get; private set; }

        public IPedidoInventarioCompromisoRepositorio pedidoInventarioCompromisos { get; private set; }

        public IEventoSignificativoSiatRepositorio eventosSignificativosSiat { get; private set; }

        public ISubVentaRepositorio subventas { get; private set; }

        public UnitWork(AppDbContext db, IProductoRepositorio productos, IElaboradoRepositorio elaborados,
                IInsumoRepositorio insumos,
                IAjusteStockRepositorio ajusteStocks,
                IRecetaRepositorio recetas,
                IMesaRepositorio mesas,
                IPedidoRepositorio pedidos,
                IRondaRepositorio rondas,
                IDetalle_RondaRepositorio detallesRondas,
                IClienteRespositorio clientes,
            ICatPaisOrigenRepositorio catPaisesOrigen,
                IOpcionRepositorio opciones,
                IComboRepositorio combo,
                IVentaRepositorio ventas,
                INotaAjusteRepositorio notasAjuste,
                IVariacionReposiotorio variacion,
                IProductoMovimientoRepositorio movimientos,
                IInsumoMovimientoRepositorio insumomovientos,
                IParaLlevarRepositorio parallevar,
                ICajaRepositorio cajas,
                ICajaMovimientoRepositorio cajamovimientos,
                ICajaHistorialRepositorio cajaHistorial,
                IProveedorRepositorio proveedores,
                IOrdenCompraRepositorio orndenes,
                IReglaBasePuntosRepositorio reglaBasePuntos,
                IAceleradorPuntosRepositorio aceleradores,
                IHistorialPuntosRepositorio historialPuntos,
                IProductoCanjeableRepositorio productosCanjeables,
                IPromocionPermanenteRepositorio promocionPermanentes,
                IPromocionPermanenteProgresoRepositorio promocionPermanenteProgresos,
                IHistorialPromocionPermanenteRepositorio historialPromocionPermanentes,
                IPromocionTemporadaRepositorio promocionTemporadas,
                IHistorialPromocionTemporadaRepositorio historialPromocionTemporadas,
                IHitoCompraRepositorio hitosCompra,
                IHistorialHitoCompraRepositorio historialHitoCompras,
                IReferidosConfigRepositorio referidosConfig,
                IHistorialReferidoRepositorio historialReferidos,
                IConfiguracionQrRepositorio configuracionQr,
                IPedidoInventarioCompromisoRepositorio pedidoInventarioCompromisos,
                IEventoSignificativoSiatRepositorio eventosSignificativosSiat,
                ISubVentaRepositorio subventas
                )
        {
            _db = db;
            this.productos = productos;
            this.elaborados = elaborados;
            this.insumos = insumos;
            ajustes = ajusteStocks;
            this.recetas = recetas;
            this.mesas = mesas;
            Pedidos = pedidos;
            this.rondas = rondas;
            this.detallesRondas = detallesRondas;
            this.clientes = clientes;
            this.catPaisesOrigen = catPaisesOrigen;
            this.opciones = opciones;
            Combo = combo;
            this.ventas = ventas;
            this.notasAjuste = notasAjuste;
            variaciones = variacion;
            this.movimientos = movimientos;
            Insumomovientos = insumomovientos;
            this.parallevar = parallevar;
            this.cajas = cajas;
            this.cajamovimientos = cajamovimientos;
            this.cajahistorial = cajaHistorial;
            this.proveedores = proveedores;
            this.ordenes = orndenes;
            this.reglaBasePuntos = reglaBasePuntos;
            this.aceleradores = aceleradores;
            this.historialPuntos = historialPuntos;
            this.productosCanjeables = productosCanjeables;
            this.promocionPermanentes = promocionPermanentes;
            this.promocionPermanenteProgresos = promocionPermanenteProgresos;
            this.historialPromocionPermanentes = historialPromocionPermanentes;
            this.promocionTemporadas = promocionTemporadas;
            this.historialPromocionTemporadas = historialPromocionTemporadas;
            this.hitosCompra = hitosCompra;
            this.historialHitoCompras = historialHitoCompras;
            this.referidosConfig = referidosConfig;
            this.historialReferidos = historialReferidos;
            this.configuracionQr = configuracionQr;
            this.pedidoInventarioCompromisos = pedidoInventarioCompromisos;
            this.eventosSignificativosSiat = eventosSignificativosSiat;
            this.subventas = subventas;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public async Task<int> SaveUnitWork()
        {
            return await _db.SaveChangesAsync();
        }
    }
}
