using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public class SolicitudAnulacionFacturaDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoDocumentoSector { get; set; }
        public int CodigoEmision { get; set; }
        public int CodigoModalidad { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int TipoFacturaDocumento { get; set; }
        public int CodigoMotivo { get; set; }
        public string Cuf { get; set; } = string.Empty;
    }

    public class RespuestaAnulacionFacturaDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    public sealed class ResultadoAnulacionFacturaDto
    {
        public bool Transaccion { get; init; }
        public int? CodigoEstado { get; init; }
        public string? CodigoDescripcion { get; init; }
        public FacturaEstado? EstadoSiat { get; init; }
        public string? ErrorMensaje { get; init; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; init; } = new();
    }
}
