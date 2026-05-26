using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IPromocionTemporadaRepositorio : IGenericRepositorio<PromocionTemporada>
    {
        IQueryable<PromocionTemporada> GetPromociones();

        /// <summary>Incluye enlaces cargados para editar y persistir cambios.</summary>
        Task<PromocionTemporada?> ObtenerConEnlacesTrackedAsync(int id);

        Task<List<PromocionTemporada>> ObtenerActivasVigentesAsync(DateTime fechaReferencia);

        Task<PromocionTemporada?> ObtenerActivaVigenteParaReclamoAsync(int id, DateTime fechaReferencia);
    }
}
