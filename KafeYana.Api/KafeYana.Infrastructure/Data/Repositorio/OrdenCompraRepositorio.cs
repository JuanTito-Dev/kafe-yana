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
    public class OrdenCompraRepositorio : GenericRepositorio<OrdenCompra>, IOrdenCompraRepositorio
    {
        private readonly DbSet<OrdenCompra> _set;
        public OrdenCompraRepositorio(AppDbContext _db) : base(_db)
        {
            _set = _db.Set<OrdenCompra>();
        }

        public async Task<OrdenCompra?> TraerOrdenCompleta(int Id)
        {
            return await _set
                .AsSplitQuery()
                .Include(x => x.Insumos)
                    .ThenInclude(x => x.Insumo)
                .Include(x => x.Productos)
                    .ThenInclude(x => x.Producto)
                        .ThenInclude(x => x.Comprado)
                .FirstOrDefaultAsync(x => x.Id == Id);
        }

        public async Task<int> Count()
        {
            return await _set.CountAsync();
        }
    }
}
