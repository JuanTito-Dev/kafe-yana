using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Prepara una NotaAjuste para ser enviada al SIAT: valida, calcula totales,
    /// genera CUF, arma XML, gzipea, hashea. Espejo de FacturaVentaSiatPreparer.
    /// </summary>
    public class NotaAjusteSiatPreparer : INotaAjusteSiatPreparer
    {
        private const decimal ToleranciaCentavos = 0.01m;

        private readonly IRecepcionNotaAjusteService _recepcionNota;
        private readonly INotaAjusteXmlGenerator _notaXmlGenerator;
        private readonly ICufdService _cufdService;
        private readonly ICufGenerator _cufGenerator;
        private readonly IFechaHoraSiatService _fechaHoraSiat;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly SiatOptions _siat;
        private readonly ILogger<NotaAjusteSiatPreparer> _logger;

        public NotaAjusteSiatPreparer(
            IRecepcionNotaAjusteService recepcionNota,
            INotaAjusteXmlGenerator notaXmlGenerator,
            ICufdService cufdService,
            ICufGenerator cufGenerator,
            IFechaHoraSiatService fechaHoraSiat,
            IDbContextFactory<AppDbContext> dbFactory,
            IOptions<SiatOptions> siatOpts,
            ILogger<NotaAjusteSiatPreparer> logger)
        {
            _recepcionNota = recepcionNota;
            _notaXmlGenerator = notaXmlGenerator;
            _cufdService = cufdService;
            _cufGenerator = cufGenerator;
            _fechaHoraSiat = fechaHoraSiat;
            _dbFactory = dbFactory;
            _siat = siatOpts.Value;
            _logger = logger;
        }

        private (int CodigoSucursal, int CodigoPuntoVenta) ResolverPuntoVentaActivo()
        {
            using var db = _dbFactory.CreateDbContext();
            var pv = db.PuntosVentaSiat
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.CodigoSucursal)
                .ThenBy(p => p.CodigoPuntoVenta)
                .FirstOrDefault();

            if (pv is not null)
                return (pv.CodigoSucursal, pv.CodigoPuntoVenta);

            return (_siat.CodigoSucursal, _siat.CodigoPuntoVenta);
        }

        public async Task PrepararNotaAsync(NotaAjuste nota, CancellationToken ct = default)
        {
            ValidarEstructura(nota);

            // Resolver el PV activo para usar en CUFD/CUF
            var pvActual = ResolverPuntoVentaActivo();

            // fechaEmision la asigna el SIAT (sincronizarFechaHora) justo después.
            DateTime fechaEmision = default;
            nota.FechaEmision = fechaEmision;
            nota.Leyenda = LeyendaSiatService.ObtenerAleatoria();
            nota.CodigoDocumentoSector = _siat.CodigoDocumentoSectorNotaAjuste;
            nota.EstadoSiat = FacturaEstado.Pendiente;
            nota.CodigoRecepcion = null;
            nota.ErrorMensaje = null;

            // CUF/CUFD — si falla el SIAT, seguimos con placeholders (igual que el preparer de facturas).
            var cuf = $"PENDIENTE-NOTA-{fechaEmision.Year}-{nota.NumeroNotaCreditoDebito:D3}";
            var cufdCodigo = "PENDIENTE";

            try
            {
                // 1) Fecha/hora oficial del SIN (bloquea si el SIAT no responde).
                fechaEmision = await _fechaHoraSiat.ObtenerFechaHoraOficialAsync(
                    pvActual.CodigoSucursal, pvActual.CodigoPuntoVenta, ct);
                // El SIAT devuelve hora BOT. La convertimos a UTC (+4h) con Kind=Utc
                // para que Npgsql pueda escribirla en la columna "timestamp with time zone".
                fechaEmision = SiatFechaEmision.ToUtcForDb(fechaEmision);

                // 2) CUFD vigente del PV activo (con la misma fechaEmision del CUF)
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    pvActual.CodigoSucursal, pvActual.CodigoPuntoVenta, fechaEmision, ct);

                cufdCodigo = cufd.Codigo;
                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmision,
                    CodigoSucursal: pvActual.CodigoSucursal,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: _siat.CodigoEmision,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumentoNotaAjuste,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSectorNotaAjuste,
                    NumeroFactura: nota.NumeroNotaCreditoDebito,
                    CodigoPuntoVenta: pvActual.CodigoPuntoVenta,
                    CodigoControl: cufd.CodigoControl));
            }
            catch (VentaException)
            {
                // Relanzar: el SIAT no respondió o rechazó fechaHora
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "CUF/CUFD no generado al preparar nota {NotaId}; se guarda PENDIENTE",
                    nota.Id);
            }

            nota.Cuf = cuf;
            nota.Cufd = cufdCodigo;

            try
            {
                var xml = _notaXmlGenerator.Generar(nota);
                var archivo = SiatGzip.ComprimirXmlABase64(xml);

                nota.XmlBase64 = archivo;
                nota.CodigoHash = _recepcionNota.CalcularHashArchivo(archivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML/archivo/hash no generado al preparar nota {NotaId}", nota.Id);
                throw new VentaException("No se pudo generar el archivo de la nota para enviar al SIAT.");
            }
        }

        private static void ValidarEstructura(NotaAjuste nota)
        {
            if (nota is null)
                throw new InvalidOperationException("La nota de ajuste es nula.");

            if (nota.IdVenta <= 0)
                throw new InvalidOperationException("La nota debe referenciar una Venta (IdVenta requerido).");

            if (nota.Detalles is null || nota.Detalles.Count == 0)
                throw new InvalidOperationException(
                    "La nota debe tener al menos un detalle.");

            // El XSD oficial (notaComputarizadaCreditoDebito.xsd, línea 215) exige
            // <xs:element name="detalle" minOccurs="2" maxOccurs="500">.
            // Cada nota DEBE contener al menos un par (trans=1 + trans=2) por producto
            // a devolver. El servicio de envío (NotaAjusteSiatEnvioService) ya garantiza
            // esta estructura antes de llegar aquí.
            if (nota.Detalles.Count < 2)
                throw new InvalidOperationException(
                    $"La nota debe tener al menos 2 detalles (XSD minOccurs=2). Actual: {nota.Detalles.Count}.");

            if (!nota.Detalles.Any(d => d.CodigoDetalleTransaccion == 1) ||
                !nota.Detalles.Any(d => d.CodigoDetalleTransaccion == 2))
                throw new InvalidOperationException(
                    "La nota debe contener al menos un detalle con codigoDetalleTransaccion=1 "
                    + "(referencia al item original) y otro con codigoDetalleTransaccion=2 (devolución).");

            // Cada detalle debe referenciar una línea original (XSD obliga).
            for (var i = 0; i < nota.Detalles.Count; i++)
            {
                var d = nota.Detalles[i];
                if (d.IdDetallePagoOriginal <= 0)
                    throw new InvalidOperationException(
                        $"Detalle #{i + 1}: IdDetallePagoOriginal es obligatorio (XSD exige referencia a la línea original).");
                if (d.NumeroLineaOriginal <= 0)
                    throw new InvalidOperationException(
                        $"Detalle #{i + 1}: NumeroLineaOriginal es obligatorio (XSD exige referencia a la línea original).");
                if (d.CodigoDetalleTransaccion <= 0)
                    throw new InvalidOperationException(
                        $"Detalle #{i + 1}: CodigoDetalleTransaccion debe ser > 0.");
            }

            // ═══ Validaciones anti-rechazo SIAT (errores 1029 / 1030 / 1031) ═══
            // 1029: suma de subtotales de líneas con codigoDetalleTransaccion=2 no cuadra
            //       con montoTotalDevuelto (el SIAT computa este valor a partir de los detalles).
            // 1030: suma de subtotales de líneas con codigoDetalleTransaccion=1 no cuadra
            //       con montoTotalOriginal. NO es venta.MontoTotal: en devoluciones parciales
            //       el SIAT espera sólo el subtotal de los items seleccionados.
            // 1031: montoEfectivoCreditoDebito no cuadra con (montoTotalDevuelto - descuento) * 0.13
            //       (es el IVA / crédito fiscal de la nota, NO el total devuelto).
            var sumaTrans1 = nota.Detalles
                .Where(d => d.CodigoDetalleTransaccion == 1)
                .Sum(d => d.SubTotal);
            if (Math.Abs(sumaTrans1 - nota.MontoTotalOriginal) > ToleranciaCentavos)
            {
                throw new InvalidOperationException(
                    $"La suma de subtotales con codigoDetalleTransaccion=1 ({sumaTrans1:0.00}) " +
                    $"no coincide con montoTotalOriginal ({nota.MontoTotalOriginal:0.00}). " +
                    $"Diferencia: {(sumaTrans1 - nota.MontoTotalOriginal):0.00}. " +
                    "Revise los detalles antes de enviar al SIAT (esto sería rechazado con error 1030).");
            }

            var sumaTrans2 = nota.Detalles
                .Where(d => d.CodigoDetalleTransaccion == 2)
                .Sum(d => d.SubTotal);
            if (Math.Abs(sumaTrans2 - nota.MontoTotalDevuelto) > ToleranciaCentavos)
            {
                throw new InvalidOperationException(
                    $"La suma de subtotales con codigoDetalleTransaccion=2 ({sumaTrans2:0.00}) " +
                    $"no coincide con montoTotalDevuelto ({nota.MontoTotalDevuelto:0.00}). " +
                    $"Diferencia: {(sumaTrans2 - nota.MontoTotalDevuelto):0.00}. " +
                    "Revise los detalles antes de enviar al SIAT (esto sería rechazado con error 1029).");
            }

            var efectivoEsperado = Math.Round(
                (nota.MontoTotalDevuelto - nota.MontoDescuentoCreditoDebito) * 0.13m,
                2, MidpointRounding.AwayFromZero);
            if (Math.Abs(nota.MontoEfectivoCreditoDebito - efectivoEsperado) > ToleranciaCentavos)
            {
                throw new InvalidOperationException(
                    $"montoEfectivoCreditoDebito ({nota.MontoEfectivoCreditoDebito:0.00}) no cuadra con " +
                    $"(montoTotalDevuelto - montoDescuentoCreditoDebito) * 0.13 ({efectivoEsperado:0.00}). " +
                    "Revise los cálculos antes de enviar al SIAT (esto sería rechazado con error 1031).");
            }

            if (nota.MontoEfectivoCreditoDebito < 0)
                throw new InvalidOperationException(
                    $"montoEfectivoCreditoDebito ({nota.MontoEfectivoCreditoDebito:0.00}) no puede ser negativo.");
        }
    }
}
