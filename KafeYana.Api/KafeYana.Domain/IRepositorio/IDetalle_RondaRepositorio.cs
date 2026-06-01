using KafeYana.Domain.Entities.Inventario;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IDetalle_RondaRepositorio : IGenericRepositorio<Detalle_ronda>
    {
        Task<Detalle_ronda?> TraerConRelacionesAsync(int idDetalle);
    }
}