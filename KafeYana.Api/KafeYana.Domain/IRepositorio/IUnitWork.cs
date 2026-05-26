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

        IOpcionRepositorio opciones { get; }

        IComboRepositorio Combo { get; }

        IVentaRepositorio ventas { get; }

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

        Task<int> SaveUnitWork();
    }
}
