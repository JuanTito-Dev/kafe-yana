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
    public class MesaRepositorio : GenericRepositorio<Mesa>, IMesaRepositorio
    {
        private readonly DbSet<Mesa> _set;
        public MesaRepositorio(AppDbContext _db) : base(_db)
        {
            _set = _db.Set<Mesa>();
        }

        public IQueryable<Mesa> MesaQuery()
        {
            return _set
                .AsNoTracking()
                .Include(x => x.pedido)
                    .ThenInclude(p => p.Rondas)
                        .ThenInclude(r => r.Detalle)
                            .ThenInclude(d => d.Opciones)
                                .ThenInclude(o => o.Opcion)
                                    .ThenInclude(op => op.Ajustes)
                                        .ThenInclude(a => a.InsumoBase)
                .Include(x => x.pedido)
                    .ThenInclude(p => p.Rondas)
                        .ThenInclude(r => r.Detalle)
                            .ThenInclude(d => d.Opciones)
                                .ThenInclude(o => o.Opcion)
                                    .ThenInclude(op => op.Ajustes)
                                        .ThenInclude(a => a.InsumoNuevo)
                .AsQueryable();
        }

        public async Task<Mesa?> GetMesaPedido(int Id)
        {
            return await _set.Include(x => x.pedido).FirstOrDefaultAsync(x => x.Id == Id);
        }

        public async Task<bool> MesaConpedido(int Id, int Id_mesa)
        {
            return await _set.AnyAsync(x => x.Id == Id_mesa && x.Id_Pedido == Id);
        }
    }
}
