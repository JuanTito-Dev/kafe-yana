using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Prepara una Venta para ser enviada al SIAT: valida, calcula CUF/CUFD,
    /// arma XML, gzipea y hashea. Espejo de <c>NotaAjusteSiatPreparer</c>.
    /// </summary>
    public class FacturaVentaSiatPreparer : IFacturaVentaSiatPreparer
    {
        private readonly IUnitWork _db;
        private readonly IRecepcionFacturaService _recepcionFactura;
        private readonly IFacturaXmlGenerator _facturaXmlGenerator;
        private readonly ICufdService _cufdService;
        private readonly ICufGenerator _cufGenerator;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICatActividadResolver _actividadResolver;
        private readonly ICatLeyendaResolver _leyendaResolver;
        private readonly SiatOptions _siat;
        private readonly ILogger<FacturaVentaSiatPreparer> _logger;

        public FacturaVentaSiatPreparer(
            IUnitWork db,
            IRecepcionFacturaService recepcionFactura,
            IFacturaXmlGenerator facturaXmlGenerator,
            ICufdService cufdService,
            ICufGenerator cufGenerator,
            IOptions<SiatOptions> siatOpts,
            IDbContextFactory<AppDbContext> dbFactory,
            ICatActividadResolver actividadResolver,
            ICatLeyendaResolver leyendaResolver,
            ILogger<FacturaVentaSiatPreparer> logger)
        {
            _db = db;
            _recepcionFactura = recepcionFactura;
            _facturaXmlGenerator = facturaXmlGenerator;
            _cufdService = cufdService;
            _cufGenerator = cufGenerator;
            _dbFactory = dbFactory;
            _actividadResolver = actividadResolver;
            _leyendaResolver = leyendaResolver;
            _siat = siatOpts.Value;
            _logger = logger;
        }

        public async Task PrepararVentaSinFacturarAsync(Venta venta, CancellationToken ct = default)
        {
            // 1) Resolver CAEB UNA vez. Lo usamos para (a) validar la matriz
            //    CAEB+Sector contra el SIN y (b) elegir la leyenda obligatoria.
            var caeb = await _actividadResolver.ResolverCaebVigenteAsync(ct);

            // Bloquear antes de quemar CUFD / incrementar numeroFactura si la
            // combinación CAEB + CodigoDocumentoSector no está habilitada por el SIN.
            await ValidarMatrizSiatAsync(caeb, _siat.CodigoDocumentoSector, ct);

            // Idem para cada código de producto de la venta: si no está vinculado
            // a la actividad económica vigente en el padrón SIN, el SIAT rechaza
            // con [1017]. Lo detectamos acá para no gastar CUFD/número en vano.
            await ValidarProductosActividadAsync(caeb, venta.Detalles, ct);

            if (venta.Facturado)
                throw new VentaException("La venta ya está marcada como facturada.");

            if (venta.Detalles.Count == 0)
                throw new VentaException("La venta no tiene detalle para generar la factura.");

            ValidarDetallesSiat(venta.Detalles);

            var numeroFactura = venta.NumeroFactura ?? await _db.ventas.SiguienteNumeroFacturaSiatAsync();

            var fechaEmision = SiatFechaEmision.AhoraUtc();

            var cuf = $"PENDIENTE-VTA-{fechaEmision.Year}-{numeroFactura:D3}";

            var cufdCodigo = "PENDIENTE";

            try
            {
                // CUF/CUFD — si falla el SIAT, seguimos con placeholders (igual que el preparer de notas).
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    _siat.CodigoSucursal, _siat.CodigoPuntoVenta, fechaEmision, ct);

                fechaEmision = cufd.FechaEmisionSolicitud;
                cufdCodigo = cufd.Codigo;

                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmision,
                    CodigoSucursal: _siat.CodigoSucursal,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: _siat.CodigoEmision,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumento,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSector,
                    NumeroFactura: numeroFactura,
                    CodigoPuntoVenta: _siat.CodigoPuntoVenta,
                    CodigoControl: cufd.CodigoControl));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CUF/CUFD no generado al facturar venta {VentaId}; se guarda PENDIENTE", venta.Id);
            }

            venta.Facturado = true;
            venta.NumeroFactura = numeroFactura;
            venta.FechaEmision = fechaEmision;
            venta.Cuf = cuf;
            venta.Cufd = cufdCodigo;

            // Leyenda obligatoria del SIN, filtrada por el CAEB principal del
            // operador. El resolver tira VentaException si CatLeyendas está
            // vacía → fail-closed (ver [[kafeyana-vservices-throw-on-missing-config]]).
            venta.Leyenda = await _leyendaResolver.ObtenerAleatoriaAsync(caeb, ct);

            venta.EstadoSiat = FacturaEstado.Pendiente;
            venta.CodigoRecepcion = null;
            venta.ErrorMensaje = null;

            try
            {
                var xml = _facturaXmlGenerator.Generar(venta);
                var archivo = SiatGzip.ComprimirXmlABase64(xml);
                venta.XmlBase64 = archivo;
                venta.CodigoHash = _recepcionFactura.CalcularHashArchivo(archivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML/archivo/hash no generado al facturar venta {VentaId}", venta.Id);
                throw new VentaException("No se pudo generar el archivo de factura para enviar al SIAT.");
            }
        }

        public async Task RegenerarVentaObservadaAsync(Venta venta, CancellationToken ct = default)
        {
            // Espejo de PrepararVentaSinFacturarAsync, pero para una venta que YA
            // tiene Facturado=true y EstadoSiat Observada/Pendiente (el SIAT nunca
            // la validó). Reusa el NumeroFactura ya asignado — NO pide uno nuevo,
            // el SIAT rechazaría un número repetido si emitiéramos otro — pero
            // regenera CUF/CUFD/XML/Hash con los datos fiscales ya corregidos en
            // la entidad (ver guard en ReenviarFacturaAsync).
            var caeb = await _actividadResolver.ResolverCaebVigenteAsync(ct);
            await ValidarMatrizSiatAsync(caeb, _siat.CodigoDocumentoSector, ct);
            await ValidarProductosActividadAsync(caeb, venta.Detalles, ct);

            if (venta.NumeroFactura is null)
                throw new VentaException("La venta no tiene número de factura asignado para regenerar.");

            ValidarDetallesSiat(venta.Detalles);

            var fechaEmision = SiatFechaEmision.AhoraUtc();
            var cuf = venta.Cuf ?? $"PENDIENTE-VTA-{fechaEmision.Year}-{venta.NumeroFactura:D3}";
            var cufdCodigo = venta.Cufd ?? "PENDIENTE";

            try
            {
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    _siat.CodigoSucursal, _siat.CodigoPuntoVenta, fechaEmision, ct);

                fechaEmision = cufd.FechaEmisionSolicitud;
                cufdCodigo = cufd.Codigo;

                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmision,
                    CodigoSucursal: _siat.CodigoSucursal,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: _siat.CodigoEmision,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumento,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSector,
                    NumeroFactura: venta.NumeroFactura.Value,
                    CodigoPuntoVenta: _siat.CodigoPuntoVenta,
                    CodigoControl: cufd.CodigoControl));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CUF/CUFD no generado al regenerar venta observada {VentaId}; se guarda PENDIENTE", venta.Id);
            }

            venta.FechaEmision = fechaEmision;
            venta.Cuf = cuf;
            venta.Cufd = cufdCodigo;
            venta.Leyenda = await _leyendaResolver.ObtenerAleatoriaAsync(caeb, ct);
            venta.EstadoSiat = FacturaEstado.Pendiente;
            venta.CodigoRecepcion = null;
            venta.ErrorMensaje = null;

            try
            {
                var xml = _facturaXmlGenerator.Generar(venta);
                var archivo = SiatGzip.ComprimirXmlABase64(xml);
                venta.XmlBase64 = archivo;
                venta.CodigoHash = _recepcionFactura.CalcularHashArchivo(archivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML/archivo/hash no generado al regenerar venta observada {VentaId}", venta.Id);
                throw new VentaException("No se pudo generar el archivo de factura para reenviar al SIAT.");
            }
        }

        private static void ValidarDetallesSiat(IEnumerable<Detalle_Pago> detalles)
        {
            foreach (var detalle in detalles)
            {
                if (detalle.UnidadMedida <= 0 || !UnidadMedidaSiatService.EsCodigoValido(detalle.UnidadMedida))
                {
                    throw new VentaException(
                        $"El producto '{detalle.Descripcion}' no tiene unidad de medida SIAT válida para facturar.");
                }
            }
        }

        /// <summary>
        /// Verifica contra la tabla CatActividadesDocumentosSector (sincronizada
        /// por SincronizadorCatActividadDocumentoSector) que el CAEB vigente
        /// pueda emitir documentos del sector indicado. Si la combinación no
        /// existe, lanza VentaException → 409 con instrucción clara al operador.
        ///
        /// Se ejecuta como PRIMER paso del preparer (antes de CUFD, CUF y XML)
        /// para no quemar recursos del SIAT en operaciones que ya sabemos
        /// inválidas.
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

        /// <summary>
        /// Verifica contra la tabla CodigosSiat (sincronizada por
        /// SincronizadorCodigosSiat vía sincronizarListaProductosServicios) que
        /// cada código de producto SIN de la venta esté vinculado, en el padrón
        /// del contribuyente, a la actividad económica (CAEB) vigente. Si falta
        /// alguno, lanza VentaException → 409 con instrucción clara, en vez de
        /// dejar que el SIAT rechace con [1017] después de gastar CUFD/número.
        /// </summary>
        private async Task ValidarProductosActividadAsync(
            string caeb, IEnumerable<Detalle_Pago> detalles, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var codigosProducto = detalles.Select(d => d.CodigoProductoSin.ToString()).Distinct().ToList();

            var habilitados = await db.CodigosSiat
                .AsNoTracking()
                .Where(c => c.CodigoActividad == caeb && codigosProducto.Contains(c.CodigoProducto))
                .Select(c => c.CodigoProducto)
                .ToListAsync(ct);

            var faltantes = detalles
                .Where(d => !habilitados.Contains(d.CodigoProductoSin.ToString()))
                .ToList();

            if (faltantes.Count > 0)
            {
                var detalle = string.Join(", ", faltantes.Select(d => $"'{d.Descripcion}' (código SIN {d.CodigoProductoSin})"));
                throw new VentaException(
                    $"El código de producto SIN no está vinculado a la actividad económica '{caeb}' para: {detalle}. "
                    + "Verifique/corrija el código SIN del producto en el catálogo (modal de búsqueda SIN) o ejecute "
                    + "POST /api/catalogos/sincronizar-productos-servicios si el código sí está habilitado pero el catálogo local está desactualizado.");
            }
        }
    }
}
