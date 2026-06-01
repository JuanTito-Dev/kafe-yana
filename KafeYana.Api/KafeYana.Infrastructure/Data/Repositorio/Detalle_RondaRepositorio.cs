using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio;

public class Detalle_RondaRepositorio(AppDbContext db) : GenericRepositorio<Detalle_ronda>(db), IDetalle_RondaRepositorio
{
    public async Task<Detalle_ronda?> TraerConRelacionesAsync(int idDetalle) =>
        await db.Set<Detalle_ronda>()
            .Include(d => d.ronda)
            .Include(d => d.Opciones)
            .Include(d => d.ItemsCombo)
            .Include(d => d.CompromisoInventario!)
                .ThenInclude(c => c.Lineas)
            .FirstOrDefaultAsync(d => d.Id == idDetalle);
}
