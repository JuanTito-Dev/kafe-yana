using System;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Servicio de recepción SOAP para notas de crédito/débito.
    /// Espejo de IRecepcionFacturaService — diferencia clave: el sobre NO incluye Cufd.
    /// </summary>
    public interface IRecepcionNotaAjusteService
    {
        string CalcularHashArchivo(string archivo);

        Task<SolicitudRecepcionNotaAjusteDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default);

        Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default);

        /// <summary>
        /// Overload para reenvío de notas emitidas en contingencia (TipoEmision=2).
        /// Acepta el <c>codigoRecepcionEventoSignificativo</c> del evento activo,
        /// que debe viajar en el sobre SOAP para que el SIAT valide la asociación.
        /// Espejo de <see cref="IRecepcionFacturaService.EnviarRecepcionContingenciaAsync"/>
        /// excepto que el sobre NO lleva Cufd (verificado contra el sobre del piloto).
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionContingenciaAsync(
            string archivo,
            string? hashArchivo,
            int codigoSucursal,
            int codigoPuntoVenta,
            string codigoRecepcionEventoSignificativo,
            CancellationToken ct = default);
    }
}