using KafeYana.Application.Dtos.HitoCompraDtos;

namespace KafeYana.Application.IServicios
{
    public interface IHitoCompraReclamoService
    {
        Task<DtoHitosReclamadosCliente> ObtenerHitosReclamadosAsync(int idCliente);

        Task<ResultadoReclamoHitoCompra> ReclamarAsync(DtoReclamarHitoCompra dto);
    }
}
