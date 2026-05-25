using KafeYana.Application.Dtos.HitoCompraDtos;

namespace KafeYana.Application.IServicios
{
    public interface IHitoCompraReclamoService
    {
        Task<ResultadoReclamoHitoCompra> ReclamarAsync(DtoReclamarHitoCompra dto);
    }
}
