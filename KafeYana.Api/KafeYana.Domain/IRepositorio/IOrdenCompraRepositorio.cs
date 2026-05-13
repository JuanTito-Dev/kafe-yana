using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IOrdenCompraRepositorio : IGenericRepositorio<OrdenCompra> 
    {
        Task<OrdenCompra?> TraerOrdenCompleta(int Id);

        Task<int> Count();
    }
}
