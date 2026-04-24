using KafeYana.Domain.Entities.Inventario;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IDetalle_RondaRepositorio : IGenericRepositorio<Detalle_ronda>
    {
        Task<bool> ExisteDetalleRondaPorProductoAsync(int idRonda, int idProducto);
        Task<Detalle_ronda?> ObtenerConOpcionesAsync(int id);
        Task<List<Detalle_ronda>> ObtenerPorRondaAsync(int idRonda);
    }
}