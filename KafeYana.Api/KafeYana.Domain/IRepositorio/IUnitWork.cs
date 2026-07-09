using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IUnitWork : IDisposable
    {
        IProductoRepositorio productos { get; }

        IElaboradoRepositorio elaborados { get; }

        IAjusteStockRepositorio ajustes { get; }

        IInsumoRepositorio insumos { get; }

        IRecetaRepositorio recetas { get; }

        IMesaRepositorio mesas { get;}

        IPedidoRepositorio Pedidos { get;}

        IRondaRepositorio rondas { get; }

        IDetalle_RondaRepositorio detallesRondas { get; }

        IClienteRespositorio clientes { get; }

        /// <summary>
        /// Catálogo paramétrico de países de origen del SIAT (<c>CatPaisOrigen</c>).
        /// Usado por <c>ClientePedidoHelper</c> para resolver el código SIN al
        /// FK local al facturar clientes extranjeros (CEX/PAS).
        /// </summary>
        ICatPaisOrigenRepositorio catPaisesOrigen { get; }

        IOpcionRepositorio opciones { get; }

        IComboRepositorio Combo { get; }

        IVentaRepositorio ventas { get; }

        INotaAjusteRepositorio notasAjuste { get; }

        IVariacionReposiotorio variaciones { get; }

        IProductoMovimientoRepositorio movimientos { get; }

        IInsumoMovimientoRepositorio Insumomovientos { get; }

        IParaLlevarRepositorio parallevar {  get; }

        ICajaRepositorio cajas { get;  }

        public ICajaMovimientoRepositorio cajamovimientos { get; }

        ICajaHistorialRepositorio cajahistorial {  get; }

        IProveedorRepositorio proveedores { get; }

        public IOrdenCompraRepositorio ordenes { get;  }

        IProductoCanjeableRepositorio productosCanjeables { get; }

        IPromocionPermanenteRepositorio promocionPermanentes { get; }

        IPromocionPermanenteProgresoRepositorio promocionPermanenteProgresos { get; }

        IHistorialPromocionPermanenteRepositorio historialPromocionPermanentes { get; }

        IPromocionTemporadaRepositorio promocionTemporadas { get; }

        IHistorialPromocionTemporadaRepositorio historialPromocionTemporadas { get; }

        IHitoCompraRepositorio hitosCompra { get; }

        IHistorialHitoCompraRepositorio historialHitoCompras { get; }

        IReferidosConfigRepositorio referidosConfig { get; }

        IHistorialReferidoRepositorio historialReferidos { get; }

        IReglaBasePuntosRepositorio reglaBasePuntos { get; }

        IAceleradorPuntosRepositorio aceleradores { get; }

        IHistorialPuntosRepositorio historialPuntos { get; }

        IConfiguracionQrRepositorio configuracionQr { get; }

        IPedidoInventarioCompromisoRepositorio pedidoInventarioCompromisos { get; }

        /// <summary>
        /// Log de eventos significativos registrados ante el SIN Bolivia.
        /// Usado por el flujo de contingencia: <c>VentaServices</c> consulta el
        /// estado antes de facturar, el wrapper detector registra eventos
        /// automáticamente. Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        IEventoSignificativoSiatRepositorio eventosSignificativosSiat { get; }

        ISubVentaRepositorio subventas { get; }

        Task<int> SaveUnitWork();
    }
}
