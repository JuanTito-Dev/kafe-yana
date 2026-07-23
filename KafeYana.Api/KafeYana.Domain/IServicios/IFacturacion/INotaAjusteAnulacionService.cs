using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Capa de servicios de facturación SOAP de bajo nivel para anulación de notas.
    /// Espejo de <see cref="IAnulacionFacturaService"/>.
    /// </summary>
    public interface INotaAjusteAnulacionService
    {
        Task<SolicitudAnulacionDocumentoAjusteDto> PrepararSolicitudAsync(
            int notaId,
            int codigoMotivo,
            CancellationToken ct = default);

        Task<RespuestaAnulacionDocumentoAjusteDto> EnviarAnulacionAsync(
            SolicitudAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default);
    }

    public interface INotaAjusteReversionAnulacionService
    {
        Task<SolicitudReversionAnulacionDocumentoAjusteDto> PrepararSolicitudAsync(
            int notaId,
            CancellationToken ct = default);

        Task<RespuestaReversionAnulacionDocumentoAjusteDto> EnviarReversionAsync(
            SolicitudReversionAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Capa de orquestación de alto nivel para anulación de notas C/D contra el SIAT.
    /// Espejo de <see cref="IFacturaSiatAnulacionService"/>.
    /// </summary>
    public interface INotaAjusteSiatAnulacionService
    {
        Task<ResultadoAnulacionNotaAjusteDto> AnularNotaAsync(
            int notaId,
            int codigoMotivo,
            CancellationToken ct = default);
    }

    public interface INotaAjusteSiatReversionAnulacionService
    {
        Task<ResultadoReversionAnulacionNotaAjusteDto> RevertirAnulacionNotaAsync(
            int notaId,
            CancellationToken ct = default);
    }
}