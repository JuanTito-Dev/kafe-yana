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

        Task<int> SaveUnitWork();
    }
}
