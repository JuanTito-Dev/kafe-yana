using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ProductoRepositorio : GenericRepositorio<Producto> ,IProductoRepositorio
    {
        public ProductoRepositorio(AppDbContext _db) : base(_db)
        {
            
        }

        public async Task<IReadOnlyList<Producto>> GetProductos(string? Nombre = null)
        {
            var query = _db.Productos.Include(x => x.Comprado).Include(x=> x.Categoria)
                .Include(x => x.Elaborado).ThenInclude(p => p.Receta).AsQueryable();

            return await query.ToListAsync();
        }

        public async Task<Producto?> TraerProducto(int Id, bool comprado = false, bool elaborado = false, bool combo = false)
        {
            var query = _db.Productos.AsQueryable();

            query = (comprado, elaborado, combo) switch
            {
                (true, false, false) => query.Where(x => x.Tipo == TiposProductos.Comprado).Include(x => x.Comprado),
                (false, true, false) => query.Where(x => x.Tipo == TiposProductos.Elaborado).Include(x => x.Elaborado),
                (false, false, true) => query.Where(x => x.Tipo == TiposProductos.Promocion).Include(x => x.Promocion),
                _ => query

            };

            return await query.FirstOrDefaultAsync(x => x.Id == Id);
        }

        
    }
}
