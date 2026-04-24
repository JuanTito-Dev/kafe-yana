using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class Detalle_RondaRepositorio : GenericRepositorio<Detalle_ronda>, IDetalle_RondaRepositorio
    {
        public Detalle_RondaRepositorio(AppDbContext db) : base(db)
        {
        }

        public async Task<bool> ExisteDetalleRondaPorProductoAsync(int idRonda, int idProducto)
        {
            return await _db.Detalle_Rondas
                .AsNoTracking()
                .AnyAsync(x => x.Id_Ronda == idRonda && x.Id_Producto == idProducto);
        }

        public async Task<Detalle_ronda?> ObtenerConOpcionesAsync(int id)
        {
            return await _db.Detalle_Rondas
                .AsNoTracking()
                .Include(x => x.Opciones)
                    .ThenInclude(x => x.Opcion)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Detalle_ronda>> ObtenerPorRondaAsync(int idRonda)
        {
            return await _db.Detalle_Rondas
                .AsNoTracking()
                .Where(x => x.Id_Ronda == idRonda)
                .Include(x => x.Opciones)
                    .ThenInclude(x => x.Opcion)
                .ToListAsync();
        }
    }
}