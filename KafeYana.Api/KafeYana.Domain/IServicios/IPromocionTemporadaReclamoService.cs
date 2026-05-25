using KafeYana.Application.Dtos.PromocionTemporadaDtos;

namespace KafeYana.Application.IServicios
{
    public interface IPromocionTemporadaReclamoService
    {
        Task<DtoPromocionesTemporadaCliente> ObtenerProductosReclamablesAsync(int idCliente);

        Task<ResultadoReclamoPromocionTemporada> ReclamarAsync(DtoReclamarPromocionTemporada dto);
    }
}
