using KafeYana.Domain.Entities;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface ISubVentaRepositorio : IGenericRepositorio<SubVenta>
    {
        Task<SubVenta?> GetByIdConDetallesAsync(int id);

        Task<System.Collections.Generic.List<SubVenta>> GetPendientesFacturarAsync(int? idMesa = null, int? idParaLlevar = null);

        Task<System.Collections.Generic.List<SubVenta>> GetByPedidoAsync(int idPedido);
    }
}
