using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IProductoRepositorio : IGenericRepositorio<Producto>
    {
        public Task<Producto?> TraerProducto(int Id, bool comprado = false, bool elaborado = false, bool combo = false);

        public Task<IReadOnlyList<Producto>> GetProductos(string? Nombre = null);
    }
}
