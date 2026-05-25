using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class PromocionPermanenteProgresoRepositorio(AppDbContext _db)
        : GenericRepositorio<PromocionPermanenteProgreso>(_db), IPromocionPermanenteProgresoRepositorio
    {
        public async Task<Dictionary<int, PromocionPermanenteProgreso>> ObtenerPorClienteYPromocionesAsync(
            int idCliente,
            IReadOnlyCollection<int> idsPromocion)
        {
            if (idsPromocion.Count == 0)
                return new Dictionary<int, PromocionPermanenteProgreso>();

            var lista = await _db.PromocionPermanenteProgresos
                .Where(p => p.Id_Cliente == idCliente && idsPromocion.Contains(p.Id_PromocionPermanente))
                .ToListAsync();

            return lista.ToDictionary(p => p.Id_PromocionPermanente);
        }
    }
}
