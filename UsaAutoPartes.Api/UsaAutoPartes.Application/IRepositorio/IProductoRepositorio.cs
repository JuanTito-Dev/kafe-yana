using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IProductoRepositorio : IGenericRepositorio<Producto>
    {
        IQueryable<Producto> GetProductos();

        Task<Producto?> GetProductoforCodigo(string codigo);
    }
}
