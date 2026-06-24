using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IReversionAnulacionFacturaService
    {
        Task<SolicitudReversionAnulacionFacturaDto> PrepararSolicitudAsync(
            string cuf,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoDocumentoSector,
            CancellationToken ct = default);

        Task<RespuestaReversionAnulacionFacturaDto> EnviarReversionAsync(
            SolicitudReversionAnulacionFacturaDto solicitud,
            CancellationToken ct = default);
    }

    public interface IFacturaSiatReversionAnulacionService
    {
        Task<ResultadoReversionAnulacionFacturaDto> RevertirAnulacionVentaAsync(
            int ventaId,
            CancellationToken ct = default);
    }
}
