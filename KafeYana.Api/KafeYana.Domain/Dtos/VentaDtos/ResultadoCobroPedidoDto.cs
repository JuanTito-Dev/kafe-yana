using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public sealed class ResultadoCobroPedidoDto
    {
        public ResultadoProcesarVenta? Resultado { get; init; }

        public ResultadoEnvioFacturaSiatDto? EnvioSiat { get; init; }

        public required string OrigenVenta { get; init; }

        public int? IdMesa { get; init; }

        public bool EsAbono { get; init; } = false;

        public decimal? MontoCubierto { get; init; }

        public DtoPedidoActualizado? PedidoActualizado { get; init; }

        public bool MesaCerrada { get; init; } = false;
    }
}
