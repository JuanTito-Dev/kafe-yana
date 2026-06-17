using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public sealed class ResultadoCobroPedidoDto
    {
        public required ResultadoProcesarVenta Resultado { get; init; }

        public ResultadoEnvioFacturaSiatDto? EnvioSiat { get; init; }

        public required string OrigenVenta { get; init; }

        public int? IdMesa { get; init; }

        public ResultadoImpresionFacturaDto? ImpresionFactura { get; init; }
    }
}
