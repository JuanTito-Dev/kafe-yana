namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public class SolicitudRecepcionFacturaDto
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
        public string Archivo { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public string HashArchivo { get; set; } = string.Empty;
    }

    public class RespuestaRecepcionFacturaDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoRecepcion { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    public class CodigoRespuestaSiatDto
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
