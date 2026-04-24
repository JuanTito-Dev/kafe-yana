using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IMesaRepositorio : IGenericRepositorio<Mesa>
    {
        IQueryable<Mesa> MesaQuery();

        Task<Mesa?> GetMesaPedido(int Id);

        Task<bool> MesaConpedido(int Id, int Id_mesa);
    }
}
