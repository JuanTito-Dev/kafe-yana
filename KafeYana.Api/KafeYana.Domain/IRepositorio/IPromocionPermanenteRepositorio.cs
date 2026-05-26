using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IPromocionPermanenteRepositorio : IGenericRepositorio<PromocionPermanente>
    {
        IQueryable<PromocionPermanente> GetPromociones();

        /// <summary>Promos activas filtradas por tipo de recompensa (consulta única al cerrar venta).</summary>
        Task<List<PromocionPermanente>> ObtenerActivasPorRecompensaAsync(string tipoRecompensa);

        Task<PromocionPermanente?> ObtenerActivaProductoGratisAsync(int idPromocionPermanente);
    }
}
