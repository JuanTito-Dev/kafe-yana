namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un motivo de anulación devuelto por el SIAT en la respuesta de
    /// `sincronizarParametricaMotivoAnulacion`.
    /// </summary>
    public class MotivoAnulacionSiatDto
    {
        /// <summary>Código numérico (1..N) que el SIN publica en su catálogo vigente.</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial (ej. "FACTURA MAL EMITIDA").</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP "sincronizarParametricaMotivoAnulacion".
    /// </summary>
    public class SincronizarMotivoAnulacionResponse
    {
        public bool Transaccion { get; set; }
        public List<MotivoAnulacionSiatDto> Motivos { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}
