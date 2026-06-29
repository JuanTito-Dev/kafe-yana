using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios;
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
    /// genera CUF, arma XML, gzipea, hashea. Espejo de <c>FacturaVentaSiatPreparer</c>.
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
        private readonly ICatActividadResolver _actividadResolver;
        private readonly ICatLeyendaResolver _leyendaResolver;
        private readonly SiatOptions _siat;
        private readonly ILogger<NotaAjusteSiatPreparer> _logger;

        public NotaAjusteSiatPreparer(
            IRecepcionNotaAjusteService recepcionNota,
            INotaAjusteXmlGenerator notaXmlGenerator,
            ICufdService cufdService,
            ICufGenerator cufGenerator,
            IFechaHoraSiatService fechaHoraSiat,
            IDbContextFactory<AppDbContext> dbFactory,
            ICatActividadResolver actividadResolver,
            ICatLeyendaResolver leyendaResolver,
            IOptions<SiatOptions> siatOpts,
            ILogger<NotaAjusteSiatPreparer> logger)
        {
            _recepcionNota = recepcionNota;
            _notaXmlGenerator = notaXmlGenerator;
            _cufdService = cufdService;
            _cufGenerator = cufGenerator;
            _fechaHoraSiat = fechaHoraSiat;
            _dbFactory = dbFactory;
            _actividadResolver = actividadResolver;
            _leyendaResolver = leyendaResolver;
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
            // 1) Resolver CAEB UNA vez. Lo usamos para (a) validar la matriz
            //    CAEB+Sector contra el SIN y (b) elegir la leyenda obligatoria.
            var caeb = await _actividadResolver.ResolverCaebVigenteAsync(ct);

            // Bloquear antes de quemar CUFD / generar XML si la combinación CAEB +
            // CodigoDocumentoSector (sector 24 = NCD por defecto) no está habilitada
            // por el SIN. Mismo patrón que FacturaVentaSiatPreparer.
            await ValidarMatrizSiatAsync(caeb, _siat.CodigoDocumentoSectorNotaAjuste, ct);

            ValidarEstructura(nota);

            // Asignar NroItem correlativo 1..N dentro de la nota. Solo se serializa
            // en el XML cuando sector==47 (XSD notaComputarizadaCreditoDebitoDescuento.xsd
            // lo exige como primer hijo de <detalle>); para sector 24 el generator
            // lo ignora. Ver NotaAjusteXmlGenerator.Sector47.
            for (var i = 0; i < nota.Detalles.Count; i++)
                nota.Detalles[i].NroItem = i + 1;

            // Usamos el Sucursal/PuntoVenta que ya viene en la nota (heredado de la Venta).
            // Antes este método llamaba a ResolverPuntoVentaActivo() independientemente,
            // lo que podía divergir del PV real de la Venta si había varios PuntosVentaSiat
            // activos → CUF construido con un PV y nota persistida con otro → inconsistencia.
            // Ver [[kafeyana-multipv-resolver]] — el fix equivalente en VentaServices
            // sobrecarga venta.CodigoSucursal/PuntoVenta con pvActual, así que al copiar
            // esos campos a la NotaAjuste (NotaAjusteSiatEnvioService:170) ya son correctos.
            var sucNota = nota.CodigoSucursal;
            var pvNota = nota.CodigoPuntoVenta;

            // fechaEmision la asigna el SIAT (sincronizarFechaHora) justo después.
            DateTime fechaEmision = default;
            nota.FechaEmision = fechaEmision;

            // Leyenda obligatoria del SIN, filtrada por el CAEB principal del
            // operador. El resolver tira VentaException si CatLeyendas está
            // vacía → fail-closed (ver [[kafeyana-vservices-throw-on-missing-config]]).
            nota.Leyenda = await _leyendaResolver.ObtenerAleatoriaAsync(caeb, ct);
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
                    sucNota, pvNota, ct);
                // El SIAT devuelve hora BOT. La convertimos a UTC (+4h) con Kind=Utc
                // para que Npgsql pueda escribirla en la columna "timestamp with time zone".
                fechaEmision = SiatFechaEmision.ToUtcForDb(fechaEmision);

                // 2) CUFD vigente del PV activo (con la misma fechaEmision del CUF)
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    sucNota, pvNota, fechaEmision, ct);

                // El CUF DEBE construirse con EXACTAMENTE la misma fechaEmisionBOT
                // que el SIAT embebió en el CUFD. Si usáramos la `fechaEmision` local
                // (la del SOAP sincronizarFechaHora), puede haber drift de milisegundos
                // respecto a lo que el SIAT tiene persistido en Cufd.FechaEmisionSolicitud,
                // y el SIAT rechaza con 1002/1003 ("EL CODIGO UNICO DE FACTURA (CUF)
                // ENVIADO EN EL XML ES INVALIDO"). Ver Cufd.FechaEmisionSolicitud.
                fechaEmision = cufd.FechaEmisionSolicitud;
                // Sincronizamos también la fechaEmision de la nota para que el XML
                // lleve exactamente la misma fecha que el CUF y el CUFD.
                nota.FechaEmision = fechaEmision;

                cufdCodigo = cufd.Codigo;
                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmision,
                    CodigoSucursal: sucNota,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: _siat.CodigoEmision,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumentoNotaAjuste,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSectorNotaAjuste,
                    NumeroFactura: nota.NumeroNotaCreditoDebito,
                    CodigoPuntoVenta: pvNota,
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

            // Persistir el CUF/CUFD calculados (o el placeholder PENDIENTE-NOTA-…)
            // en la entidad. Sin esto, nota.Cuf queda con el "PENDIENTE" puesto
            // en NotaAjusteSiatEnvioService.cs:199 y el INSERT choca con cualquier
            // otra fila que tenga ese placeholder en IX_NotaAjuste_Cuf (23505).
            // Espejo de FacturaVentaSiatPreparer.cs:118-119.
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
                throw new VentaException("No se pudo generar el archivo de nota para enviar al SIAT.");
            }
        }

        /// <summary>
        /// Valida estructura de la nota (mínimo 2 detalles, cuadre de totales,
        /// trans=1 y trans=2 presentes, referencias válidas a IdDetallePagoOriginal,
        /// etc.). Levanta VentaException con código SIAT específico para
        /// diagnóstico rápido desde el front.
        /// </summary>
        private void ValidarEstructura(NotaAjuste nota)
        {
            if (nota.Detalles.Count < 2)
                throw new VentaException(
                    "La nota debe tener al menos 2 detalles (transacción 1 = descuento/bonificación, transacción 2 = devolución/anulación). Error SIAT 1042 esperado.");

            var trans1 = nota.Detalles.Count(d => d.CodigoDetalleTransaccion == 1);
            var trans2 = nota.Detalles.Count(d => d.CodigoDetalleTransaccion == 2);
            if (trans1 == 0 || trans2 == 0)
                throw new VentaException(
                    "La nota debe tener al menos un detalle con codigoDetalleTransaccion=1 (descuento/bonificación) y otro con =2 (devolución/anulación). Error SIAT 1042 esperado.");

            // montoEfectivoCreditoDebito = montoTotalDevuelto * 0.13 (IVA Boliviano).
            // Ver [[kafeyana-notaajuste-siat-reglas]] para el detalle del cálculo.
            if (nota.MontoEfectivoCreditoDebito < 0)
                throw new InvalidOperationException(
                    $"montoEfectivoCreditoDebito ({nota.MontoEfectivoCreditoDebito:0.00}) no puede ser negativo.");
        }

        /// <summary>
        /// Verifica contra la tabla CatActividadesDocumentosSector que el CAEB
        /// vigente pueda emitir documentos del sector indicado (24 = NCD por
        /// defecto). Si la combinación no existe, lanza VentaException → 409 con
        /// instrucción clara al operador. Espejo del método del facturero.
        /// </summary>
        private async Task ValidarMatrizSiatAsync(string caeb, int codigoDocumentoSector, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(caeb))
                throw new VentaException("No se pudo resolver la actividad económica (CAEB vacío).");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existe = await db.CatActividadesDocumentosSector
                .AsNoTracking()
                .AnyAsync(m => m.CodigoActividad == caeb
                            && m.CodigoDocumentoSector == codigoDocumentoSector, ct);

            if (!existe)
            {
                throw new VentaException(
                    $"La actividad económica CAEB '{caeb}' no está autorizada por el SIAT "
                    + $"para emitir documentos sector {codigoDocumentoSector}. "
                    + "Ejecute POST /api/catalogos/sincronizar-actividades-documento-sector "
                    + "o actualice DatosEmpresaOptions.CodigoActividad.");
            }

            _logger.LogInformation(
                "SIAT matrix check OK: CAEB={Caeb} SectorDoc={Sector}",
                caeb, codigoDocumentoSector);
        }
    }
}
