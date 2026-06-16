using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IAnulacionFacturaService
    {
        Task<SolicitudAnulacionFacturaDto> PrepararSolicitudAsync(
            string cuf,
            int codigoMotivo,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoDocumentoSector,
            CancellationToken ct = default);

        Task<RespuestaAnulacionFacturaDto> EnviarAnulacionAsync(
            SolicitudAnulacionFacturaDto solicitud,
            CancellationToken ct = default);
    }

    public interface IFacturaSiatAnulacionService
    {
        Task<ResultadoAnulacionFacturaDto> AnularVentaAsync(
            int ventaId,
            int codigoMotivo,
            CancellationToken ct = default);
    }
}
