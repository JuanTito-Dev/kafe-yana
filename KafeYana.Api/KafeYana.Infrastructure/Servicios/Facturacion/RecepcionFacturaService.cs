using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class RecepcionFacturaService : IRecepcionFacturaService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<RecepcionFacturaService> _logger;

        public RecepcionFacturaService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<RecepcionFacturaService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
        }

        public string CalcularHashArchivo(string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de factura es requerido para calcular el hash.", nameof(archivo));

            return SiatSha256.GenerarHashArchivo(archivo);
        }

        public async Task<SolicitudRecepcionFacturaDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de factura es requerido.", nameof(archivo));

            // Si el caller no pasa fechaEmision, usamos la hora UTC actual.
            // Lo importante es que el CUFD que se consulte/vuelva a pedir coincida
            // con la fechaEmision usada al generar el CUF (error 1002/1003 si no).
            var fechaEmisionRef = fechaEmision ?? SiatFechaEmision.AhoraUtc();

            // Sucursal/PV efectivos: si el caller los prefijó (caso del cobro), se usan
            // esos. Si no, se cae a appsettings.json como antes.
            var sucEfectiva = codigoSucursal ?? _opts.CodigoSucursal;
            var pvEfectivo = codigoPuntoVenta ?? _opts.CodigoPuntoVenta;

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(sucEfectiva, pvEfectivo, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de facturar.");

            // Si el caller prefijó el CUFD (lo usó para generar el CUF en la misma
            // operación), lo reusamos tal cual para evitar la divergencia entre el
            // CUF embebido en el XML y el CUFD del sobre SOAP (errores 1002/1003).
            // Si no, hacemos la consulta independiente legacy.
            string cufdCodigo;
            if (!string.IsNullOrWhiteSpace(cufdPrefijo))
            {
                cufdCodigo = cufdPrefijo.Trim();
                _logger.LogInformation(
                    "RecepcionFactura usando CUFD prefijado por el caller ({Cufd}) para mantener consistencia con el CUF",
                    cufdCodigo);
            }
            else
            {
                _logger.LogWarning(
                    "RecepcionFactura SIN cufdPrefijo — haciendo fetch independiente (legacy path). "
                    + "Si el CUF fue generado con un CUFD distinto al que se obtenga aquí, "
                    + "el SIAT rechazará con 1002/1003.");

                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    sucEfectiva, pvEfectivo, fechaEmisionRef, ct);

                if (!cufd.EsVigente())
                    throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de facturar.");

                cufdCodigo = cufd.Codigo;
            }

            hashArchivo = string.IsNullOrWhiteSpace(hashArchivo)
                ? CalcularHashArchivo(archivo)
                : hashArchivo.Trim();

            var solicitud = new SolicitudRecepcionFacturaDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSector,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = pvEfectivo,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = sucEfectiva,
                Cufd = cufdCodigo,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                Archivo = archivo,
                HashArchivo = hashArchivo,
                FechaEnvio = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Solicitud RecepcionFactura preparada. HashArchivo={Hash}. Suc={Suc}, PV={PV}. CUIS vigente hasta {CuisVigencia}, CUFD={Cufd}",
                hashArchivo, sucEfectiva, pvEfectivo, cuis.FechaVigencia, cufdCodigo);

            return solicitud;
        }

        public async Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            var dto = await PrepararSolicitudAsync(
                archivo, hashArchivo, fechaEmision,
                cufdPrefijo, codigoSucursal, codigoPuntoVenta, ct);
            var respuesta = await _siat.RecepcionFacturaAsync(dto, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó RecepcionFactura. Estado={Estado}. Mensajes: {Errores}",
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "RecepcionFactura aceptada. CodigoRecepcion={Codigo}",
                    respuesta.CodigoRecepcion);
            }

            return respuesta;
        }
    }
}
