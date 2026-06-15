namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface ICufGenerator
    {
        /// <summary>
        /// Genera el CUF según algoritmo SIAT (módulo 11 + Base16 + código control CUFD).
        /// </summary>
        string Generar(CufGeneracionRequest request);
    }

    public record CufGeneracionRequest(
        long Nit,
        DateTime FechaEmision,
        int CodigoSucursal,
        int CodigoModalidad,
        int TipoEmision,
        int TipoFacturaDocumento,
        int CodigoDocumentoSector,
        long NumeroFactura,
        int CodigoPuntoVenta,
        string CodigoControl);
}
