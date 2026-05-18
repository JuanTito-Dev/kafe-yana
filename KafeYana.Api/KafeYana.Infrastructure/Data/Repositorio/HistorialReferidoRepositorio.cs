using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HistorialReferidoRepositorio(AppDbContext db)
        : GenericRepositorio<HistorialReferido>(db), IHistorialReferidoRepositorio
    {
        public IQueryable<HistorialReferido> GetHistorial()
        {
            return _db.HistorialReferidos.AsNoTracking().OrderByDescending(x => x.Fecha).AsQueryable();
        }
    }
}
