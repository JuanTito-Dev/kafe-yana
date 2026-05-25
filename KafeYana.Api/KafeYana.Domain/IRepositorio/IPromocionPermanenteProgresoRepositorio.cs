using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IPromocionPermanenteProgresoRepositorio : IGenericRepositorio<PromocionPermanenteProgreso>
    {
        /// <summary>Progreso tracked por promo para un cliente (una consulta).</summary>
        Task<Dictionary<int, PromocionPermanenteProgreso>> ObtenerPorClienteYPromocionesAsync(
            int idCliente,
            IReadOnlyCollection<int> idsPromocion);
    }
}
