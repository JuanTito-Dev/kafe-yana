using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public sealed class ResultadoEnvioFacturaSiatDto
    {
        public bool Enviado { get; init; }

        public bool Transaccion { get; init; }

        public FacturaEstado? EstadoSiat { get; init; }

        public int? CodigoEstado { get; init; }

        public string? CodigoRecepcion { get; init; }

        public string? CodigoDescripcion { get; init; }

        public string? ErrorMensaje { get; init; }

        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; init; } = new();
    }
}
