using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IParaLlevarRepositorio : IGenericRepositorio<ParaLlevar>
    {
        Task<ParaLlevar?> GetParaLlevarConPedido();

        Task<ParaLlevar?> GetPorPedidoActivoAsync(int idPedido);

        Task<bool> TienePedidoActivo();

        IQueryable<ParaLlevar> ParaLlevarQuery();
    }
}
