using KafeYana.Application.Dtos.ImpresoraDtos;

namespace KafeYana.Application.IServicios
{
    public interface IImpresoraService
    {
        Task<List<ResultadoImpresoraDto>> EnviarPedidoAsync(PedidoImprimirDto dto);
        Task<List<ResultadoImpresoraDto>> EnviarCuentaAsync(CuentaImprimirDto dto);
        Task<List<ResultadoImpresoraDto>> EnviarReciboAsync(ReciboImprimirDto dto);
        Task EnviarCatalogoAsync(CatalogoImprimirDto dto);
    }
}
