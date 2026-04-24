using KafeYana.Application.Dtos.CompradoDtos;
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

        public async Task<Producto?> GetComprado(int Id)
        {
            var producto = await _db.Productos.Include(x => x.Comprado)
                .FirstOrDefaultAsync(x => x.Id == Id && x.Tipo == TiposProductos.Comprado);

            return producto != null? producto : null;
        }

        public IQueryable<Comprado> GetComprados()
        {
            return _db.Comprados.AsNoTracking().AsSplitQuery().AsQueryable();
        }

        public async Task<Comprado?> GetCompradowithproducto(int id)
        {
            var comprado = await _db.Comprados.Include(x => x.Producto).FirstOrDefaultAsync(x => x.Id_Producto == id);

            return comprado;
        }

        public async Task<IReadOnlyList<Producto>> GetProductos(string? tipo = null, string? categoria = null, string? Nombre = null)
        {
            var query = _db.Productos
            .Include(x => x.Comprado)
            .Include(x => x.Categoria)
            .Include(x => x.Elaborado)
                .ThenInclude(p => p.Receta)
                .AsQueryable();

            // 🔹 Filtro por tipo
            if (!string.IsNullOrWhiteSpace(tipo))
            {
                query = query.Where(x => x.Tipo == tipo);
            }

            // 🔹 Filtro por categoría
            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(x => x.Categoria.Nombre == categoria);
            }

            // 🔹 Búsqueda flexible (tipo "contiene")
            if (!string.IsNullOrWhiteSpace(Nombre))
            {
                query = query.Where(x =>
                    x.Nombre.Contains(Nombre) ||
                    x.Descripcion.Contains(Nombre)
                );
            }

            return await query.ToListAsync();
        }

        

        
    }
}
