using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class RecetaRepositorio : GenericRepositorio<Receta>, IRecetaRepositorio
    {
        public RecetaRepositorio(AppDbContext _db) : base(_db)
        {
        }

        public async Task<Receta?> GetReceta(int Id, bool includeProducto = false)
        {
            var query = _db.Recetas.Include(x => x.Detalles).ThenInclude(x => x.Insumo).AsQueryable();
            if (includeProducto)
            {
                query = query.Include(x => x.Elaborado);
            }

            var receta = await query.FirstOrDefaultAsync(r => r.Id == Id); 

            return receta;
        }

        public IQueryable<Receta> GetRecetas()
        {
            return _db.Recetas.AsNoTracking().AsSplitQuery().AsQueryable();
        }

        public async Task<Receta?> GetRectaByIdElaborado(int Id)
        {
            return await _db.Recetas.Include(x => x.Detalles).ThenInclude(x => x.Insumo).FirstOrDefaultAsync(x => x.Id_Elaborado == Id);
        }
    }
}

