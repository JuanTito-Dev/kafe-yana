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
    public class ComboRepositorio : GenericRepositorio<Producto>, IComboRepositorio
    {
        public ComboRepositorio(AppDbContext _db) : base(_db)
        {
            
        }

        public async Task<Producto?> GetCombo(int Id)
        {
            var producto = await _db.Productos.Include(x => x.Promocion)
                .ThenInclude(x => x.Detalles).
                FirstOrDefaultAsync(x => x.Id == Id && x.Tipo == TiposProductos.Promocion);

            return producto != null? producto : null;
        }

        public async Task<Promocion?> TraerPromocion(int Id)
        {
            return await _db.Promociones.Include(x => x.Detalles)
                .ThenInclude(x => x.Producto)
                .FirstOrDefaultAsync(x => x.Producto_Id == Id);
        }

        public IQueryable<Promocion> GetCombos()
        {
            return _db.Promociones.AsNoTracking().AsSplitQuery().AsQueryable();
        }
    }
}
