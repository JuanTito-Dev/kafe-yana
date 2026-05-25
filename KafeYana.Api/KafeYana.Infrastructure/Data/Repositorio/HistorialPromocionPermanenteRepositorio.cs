using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HistorialPromocionPermanenteRepositorio(AppDbContext _db)
        : GenericRepositorio<HistorialPromocionPermanente>(_db), IHistorialPromocionPermanenteRepositorio
    {
    }
}
