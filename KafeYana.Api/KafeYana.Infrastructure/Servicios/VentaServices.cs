using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios
{
    public class VentaServices(
        IUnitWork _db,
        IPuntosService _puntos,
        IPromocionPermanenteVentaService _promocionPermanenteVenta,
        IPromocionPermanenteDescuentoService _promocionDescuento,
        IPromocionPermanenteProductoGratisService _productoGratis,
        IInventarioPedidoCompromisoService _inventarioPedidoCompromiso,
        IRecepcionFacturaService _recepcionFactura,
        IFacturaXmlGenerator _facturaXmlGenerator,
        ICufdService _cufdService,
        ICufGenerator _cufGenerator,
        IFechaHoraSiatService _fechaHoraSiat,
        IEventoSignificativoSiatService _eventoSignificativoSiat,
        IOptions<SiatOptions> siatOpts,
        IOptions<DatosEmpresaOptions> empresaOpts,
        IDbContextFactory<AppDbContext> dbFactory,
        ICatActividadResolver actividadResolver,
        ICatLeyendaResolver catLeyendaResolver,
        ILogger<VentaServices> logger) : IVentaServices
    {
        private readonly SiatOptions _siat = siatOpts.Value;
        private readonly DatosEmpresaOptions _empresa = empresaOpts.Value;
        private readonly ICatActividadResolver _actividadResolver = actividadResolver;

        public async Task<ResultadoProcesarVenta> ProcesarVenta(DtoVentaPedido datos, string cajero)
        {
            if (string.IsNullOrWhiteSpace(cajero))
                throw new VentaException("Usuario cajero no identificado.");

            var pedido = await _db.Pedidos.TraerPedido(datos.Id_Pedido);
            if (pedido is null)
                throw new InventarioException("Pedido no encontrado.");

            if (pedido.Rondas.Count == 0 || !pedido.Rondas.Any(r => r.Detalle.Count > 0))
                throw new VentaException("El pedido no tiene productos para cobrar.");

            var (cliente, numeroDocumento) = await ClientePedidoHelper.ResolverClienteParaCobroAsync(
                _db, datos, pedido);

            if (!cliente.Estado)
                throw new VentaException("El cliente está inactivo y no puede realizarse el cobro.");

            // Resolver el (sucursal, puntoVenta) activo UNA VEZ por request.
            // Si el frontend lo envió (vía navbar), se valida contra BD y se usa.
            // Si NO, se cae al comportamiento legacy: primer PV activo o appsettings.
            // Ver [[kafeyana-multipv-resolver]] — el resolver legacy era frágil
            // porque si había varios PVs activos tomaba siempre el primero y el
            // PV del CUF no coincidía con el PV del sobre SOAP → SIAT 1002/1003.
            var pvActual = await ResolverPuntoVentaParaCobroAsync(datos);

            // fechaEmision la asigna el SIAT dentro de ConstruirVentaFacturadaAsync
            // (así garantizamos que fechaEmision y CUF usen la MISMA hora oficial).
            DateTime fechaEmision = default;
            var anio = SiatFechaEmision.AhoraUtc().Year; // para codigoVenta humano, no para SIAT
            long numeroFacturaSiat = 0;
            string codigoVenta;
            if (datos.Factura)
            {
                numeroFacturaSiat = await _db.ventas.SiguienteNumeroFacturaSiatAsync() /*+ 20*/;
                codigoVenta = GenerarCodigoVentaFacturada(anio, numeroFacturaSiat);
            }
            else
            {
                // Guid.NewGuid():N garantiza unicidad aunque el frontend reintente
                // o haga doble clic (el milisegundo podría colisionar).
                codigoVenta = $"VTA-{anio}-C{pedido.Id}-{Guid.NewGuid():N}";
            }

            await _inventarioPedidoCompromiso.AplicarMovimientosYCerrarAsync(datos.Id_Pedido, codigoVenta);

            // Resolvemos el CAEB UNA vez por venta (no por línea) — un cobro siempre
            // se imputa a la misma actividad económica. Esto además evita N consultas
            // a CatActividades en el bucle de ConstruirDetalles.
            var actividadEconomica = await _actividadResolver.ResolverCaebVigenteAsync();

            var (detallesVenta, tieneCombo) = ConstruirDetalles(pedido, validarUnidadSiat: datos.Factura, actividadEconomica);
            if (detallesVenta.Count == 0)
                throw new VentaException("No se pudo armar el detalle de la venta. Verifique los productos del pedido.");

            var subtotal = pedido.Rondas.Sum(r => r.SubTotal);
            if (subtotal <= 0)
                throw new VentaException("El total del pedido debe ser mayor a cero.");

            var (totalCobrar, descuento) = await ResolverTotalCobrarAsync(datos, cliente, subtotal, codigoVenta);

            if (datos.Pagos.Total != totalCobrar)
            {
                var esperado = datos.AplicarDescuentos
                    ? $"total con descuento ({totalCobrar:F2})"
                    : $"total del pedido ({totalCobrar:F2})";

                throw new InventarioException($"El total de los pagos no coincide con el {esperado}.");
            }

            var venta = datos.Factura
                ? await ConstruirVentaFacturadaAsync(datos, cajero, cliente, numeroDocumento, fechaEmision, numeroFacturaSiat, totalCobrar, descuento, detallesVenta, pvActual, actividadEconomica)
                : ConstruirVentaSinFactura(datos, cajero, cliente, numeroDocumento, fechaEmision, codigoVenta, totalCobrar, descuento, detallesVenta);

            await _db.Pedidos.Remove(pedido);

            var puntosPorVenta = await _puntos.CalcularYAplicarPuntosAsync(cliente, subtotal, tieneCombo, codigoVenta);
            var promocionPermanente = await _promocionPermanenteVenta.ProcesarAlFinalizarVentaAsync(
                cliente, subtotal, codigoVenta);

            await _productoGratis.RegistrarProgresoPostVentaAsync(cliente.Id, subtotal);
            cliente.RegistrarCompra();

            return new ResultadoProcesarVenta
            {
                Venta = venta,
                PuntosPorVenta = puntosPorVenta,
                PromocionPermanente = promocionPermanente,
                DescuentoPromocion = descuento
            };
        }

        private static string GenerarCodigoVentaFacturada(int anio, long numeroFactura) =>
            $"VTA-{anio}-{numeroFactura:D3}";

        private async Task<Venta> ConstruirVentaFacturadaAsync(
            DtoVentaPedido datos,
            string cajero,
            Cliente cliente,
            string numeroDocumento,
            DateTime fechaEmision,
            long numeroFactura,
            decimal totalCobrar,
            ResultadoAplicacionDescuentoPromocion? descuento,
            List<Detalle_Pago> detallesVenta,
            (int CodigoSucursal, int CodigoPuntoVenta) pvActual,
            string actividadEconomica)
        {
            // ─── 0) Chequeo upfront de contingencia ─────────────────────────
            // Si hay una contingencia activa para este PV, emitimos en modo
            // offline (TipoEmision=2) SIN tocar el SIAT para CUIS/CUFD/FechaHora.
            // El sobre SOAP se enviará al recuperar la conexión, citando el
            // CodigoRecepcionEventoSignificativo del evento activo.
            // Ver [[kafeyana-contingencia-siat]].
            var contingencia = await _eventoSignificativoSiat
                .ObtenerEstadoContingenciaAsync(pvActual.CodigoSucursal, pvActual.CodigoPuntoVenta);

            if (contingencia.ContingenciaActiva)
            {
                logger.LogInformation(
                    "Contingencia activa detectada. Emitiendo VentaId=N/A en modo offline "
                  + "(tipoEmision=4, EventoId={Id}, Suc={Suc}, PV={PV})",
                    contingencia.EventoSignificativoId,
                    pvActual.CodigoSucursal, pvActual.CodigoPuntoVenta);

                return await ConstruirVentaOfflineAsync(
                    datos, cajero, cliente, numeroDocumento, numeroFactura, totalCobrar,
                    descuento, detallesVenta, pvActual, actividadEconomica, contingencia);
            }

            // ─── Generar CUF/CUFD REAL antes de armar la venta ─────────────────
            // Si la generación falla, lanzamos excepción para que la transacción
            // de EjecutarCobroAsync haga rollback y la venta NO quede persistida
            // con un placeholder "PENDIENTE-VTA-...". Un placeholder así bloquearía
            // cualquier reintento por colisión en IX_Venta_Cuf si el numeroFactura
            // se reutilizara (race condition histórica, ya mitigada con sequence).
            string cuf;
            string cufdCodigo;
            try
            {
                // 1) Obtener fecha/hora oficial del SIN antes de generar el CUF.
                //    Si el SIAT está caído, esto lanza VentaException y bloquea el cobro
                //    (no usamos hora local porque sería rechazada).
                fechaEmision = await _fechaHoraSiat.ObtenerFechaHoraOficialAsync(
                    pvActual.CodigoSucursal,
                    pvActual.CodigoPuntoVenta);
                // El SIAT devuelve hora BOT. La convertimos a UTC (+4h) con Kind=Utc
                // para que Npgsql pueda escribirla en la columna "timestamp with time zone".
                // El XML recibe la hora BOT original (vía SiatFechaEmision.Formatear).
                fechaEmision = SiatFechaEmision.ToUtcForDb(fechaEmision);

                // 2) Obtener CUFD vigente (con la misma fechaEmision que usaremos en el CUF)
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    pvActual.CodigoSucursal,
                    pvActual.CodigoPuntoVenta,
                    fechaEmision);

                cufdCodigo = cufd.Codigo;
                // El CUF DEBE construirse con EXACTAMENTE la misma fechaEmision que
                // el SIAT embebió en el CUFD (mismo patrón que NotaAjusteSiatPreparer:145
                // y FacturaVentaSiatPreparer:95; si difiere, el SIAT rechaza con
                // 1002/1003). Ver [[kafeyana-cuf-cufd-fechaemision]].
                fechaEmision = cufd.FechaEmisionSolicitud;
                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmision,
                    CodigoSucursal: pvActual.CodigoSucursal,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: _siat.CodigoEmision,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumento,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSector,
                    NumeroFactura: numeroFactura,
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
                var detalle = ex.Message;
                if (ex.InnerException is not null)
                    detalle = $"{detalle} → Inner: {ex.InnerException.Message}";

                // Pieza 4 — fallback reactivo en el flujo de cobro. Antes de
                // propagar el error, intenta activar contingencia local y
                // redirigir a ConstruirVentaOfflineAsync. Cubre DOS casos que
                // las piezas 1-3 no resuelven por sí solas:
                //   (a) Primer cobro con SIAT caído: el monitor todavía no
                //       cruzó el umbral (FallosConsecutivos=1 < 2), así que
                //       DispararContingenciaAutomaticaAsync nunca se llamó.
                //   (b) Segundo cobro, monitor disparó pieza 2 pero en otra
                //       thread — esta request no puede esperar a que termine.
                var contingenciaReactiva = await IntentarActivarContingenciaReactivaAsync(
                    pvActual.CodigoSucursal, pvActual.CodigoPuntoVenta, default);

                if (contingenciaReactiva is not null)
                {
                    logger.LogWarning(
                        "CUF/CUFD falló ({Detalle}) pero contingencia reactiva activa Id={Id}. "
                      + "Redirigiendo venta a modo offline (TipoEmision=2).",
                        detalle, contingenciaReactiva.EventoSignificativoId);

                    return await ConstruirVentaOfflineAsync(
                        datos, cajero, cliente, numeroDocumento, numeroFactura, totalCobrar,
                        descuento, detallesVenta, pvActual, actividadEconomica,
                        contingenciaReactiva);
                }

                logger.LogError(
                    ex,
                    "CUF/CUFD no generado al facturar número {NumeroFactura}; "
                  + "abortando cobro para no persistir PENDIENTE",
                    numeroFactura);
                throw new VentaException(
                    "No se pudo generar el CUF/CUFD para la factura. "
                    + "El CUFD puede haber vencido o el SIAT no responde. "
                    + $"Detalle: {detalle}");
            }

            var venta = CrearVentaBase(
                datos, cajero, cliente, numeroDocumento, fechaEmision, totalCobrar, descuento, detallesVenta);

            // FIX: CrearVentaBase usa _siat.CodigoSucursal/_siat.CodigoPuntoVenta
            // (de appsettings.json), pero el CUF y el CUFD que acabamos de generar
            // se construyeron con pvActual (de la tabla PuntosVentaSiat en BD).
            // Si esos valores difieren (caso típico: hay varios PuntosVentaSiat
            // activos y el cobro está usando uno distinto al de appsettings), la
            // Venta queda internamente inconsistente:
            //   - venta.Cuf         → tiene PV de pvActual (ej. 1) embebido en el CUF
            //   - venta.Cufd        → es un CUFD emitido para (pvActual.Suc, pvActual.PV)
            //   - venta.CodigoPuntoVenta → queda en appsettings (ej. 0)
            // Cuando FacturaSiatEnvioService envía el sobre SOAP con
            // venta.CodigoPuntoVenta, el SIAT busca el CUFD vigente para esa
            // (Suc, PV) y compara su CodigoControl contra el del CUF → 1002/1003.
            // Sobrescribimos con pvActual para que los 3 valores estén alineados.
            venta.CodigoSucursal = pvActual.CodigoSucursal;
            venta.CodigoPuntoVenta = pvActual.CodigoPuntoVenta;

            venta.Facturado = true;
            venta.NumeroFactura = numeroFactura;
            venta.Cuf = cuf;
            venta.Cufd = cufdCodigo;
            venta.CodigoTipoDocumentoIdentidad = datos.CodigoTipoDocumento!.Value;

            // Leyenda obligatoria del SIN, filtrada por el CAEB del operador.
            // El resolver tira VentaException si CatLeyendas está vacía → fail-closed
            // (ver [[kafeyana-vservices-throw-on-missing-config]]).
            venta.Leyenda = await catLeyendaResolver.ObtenerAleatoriaAsync(actividadEconomica, default);

            venta.EstadoSiat = FacturaEstado.Pendiente;

            try
            {
                var xml = _facturaXmlGenerator.Generar(venta);
                var archivo = SiatGzip.ComprimirXmlABase64(xml);
                venta.XmlBase64 = archivo;
                venta.CodigoHash = _recepcionFactura.CalcularHashArchivo(archivo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "XML/archivo/hash de factura no generado");
                throw new InventarioException("No se pudo generar el archivo de factura para enviar al SIAT.");
            }

            return venta;
        }

        /// <summary>
        /// Construye una Venta para emisión offline durante contingencia SIAT.
        /// NO consulta CUIS/CUFD/FechaHora en línea (el SIAT está caído).
        /// Reutiliza el <c>CufdEvento</c> guardado en el evento significativo activo
        /// y la <c>FechaHoraInicioEvento</c> como fechaEmision para que el CUF
        /// sea consistente con el CUFD que el SIAT asoció al evento.
        ///
        /// La venta queda con <c>EstadoSiat=Pendiente</c> y <c>CodigoRecepcion=null</c>;
        /// el envío al SIAT se difiere para cuando se recupere la conexión
        /// (ver <c>ReenvioFacturasContingenciaService</c>).
        ///
        /// Ver [[kafeyana-contingencia-siat]] y [[kafeyana-cuf-cufd-fechaemision]].
        /// </summary>
        private async Task<Venta> ConstruirVentaOfflineAsync(
            DtoVentaPedido datos,
            string cajero,
            Cliente cliente,
            string numeroDocumento,
            long numeroFactura,
            decimal totalCobrar,
            ResultadoAplicacionDescuentoPromocion? descuento,
            List<Detalle_Pago> detallesVenta,
            (int CodigoSucursal, int CodigoPuntoVenta) pvActual,
            string actividadEconomica,
            EstadoContingenciaDto contingencia)
        {
            if (contingencia.EventoSignificativoId is not int eventoId)
                throw new VentaException("Estado de contingencia inconsistente: sin EventoSignificativoId.");

            // Para el CUF en contingencia usamos TipoEmision=2 (Contingencia computarizada).
            // El resto de campos del CUF se mantienen iguales.
            // Para CodigoModalidad=2 (Computarizada), codigoEmision=2 = "Computarizada fuera de línea"
            // según Resolución Normativa 102100000028.
            var tipoEmisionContingencia = 2;

            // El CUF se construye como en línea: hex(53d + módulo11) en base16 + CodigoControl
            // (hex 15-16 chars) del CUFD embebido al registrar el evento. Ver
            // [[kafeyana-cuf-cufd-fechaemision]]. El CufdEvento (base64) viaja
            // como parámetro SOAP del sobre de la factura contingencia, NO como
            // CodigoControl del CUF (Gap 7 — antes del fix esto estaba mal y
            // producía CUFs con base64 al final, rechazados por el SIAT).
            //
            // Fallback defensivo: si CodigoControlEvento es null (evento pre-Gap-7
            // o edge case), se cae al CufdEvento para no romper el flujo durante
            // el rollout. Las ventas emitidas con el fallback tendrán CUF
            // malformado igual que antes — el sweep de Gap 7 las marcará con
            // ErrorMensaje pidiendo anulación manual.
            var cufdCodigo = contingencia.CufdEvento ?? string.Empty;
            var codigoControl = contingencia.CodigoControlEvento
                ?? contingencia.CufdEvento
                ?? string.Empty;
            var fechaEmisionUtc = contingencia.FechaHoraInicioEvento ?? DateTime.UtcNow;

            // Asegurar que la fecha en memoria tenga Kind=Utc para BD. La columna
            // "FechaEmision" de Venta es timestamptz — Npgsql rechaza Kind=Unspecified
            // o Kind=Local (https://www.npgsql.org/doc/types/datetime.html).
            // Si vino con Kind=Unspecified desde algún path anterior (ej: helper
            // externo), la tratamos como UTC sin conversión.
            var fechaEmisionParaBd = fechaEmisionUtc.Kind switch
            {
                DateTimeKind.Utc => fechaEmisionUtc,
                DateTimeKind.Local => fechaEmisionUtc.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fechaEmisionUtc, DateTimeKind.Utc)
            };

            // El CUF DEBE llevar la fecha BOT (UTC-4) — el SIAT embebió la fecha
            // BOT en el CUFD del evento, no UTC. Mismo patrón que el flujo online
            // respeta vía SiatFechaEmision.Formatear / ToUtcForDb (ver
            // [[kafeyana-cuf-cufd-fechaemision]]).
            var fechaEmisionCuf = fechaEmisionParaBd.AddHours(-4);
            fechaEmisionCuf = DateTime.SpecifyKind(fechaEmisionCuf, DateTimeKind.Unspecified);

            string cuf;
            try
            {
                cuf = _cufGenerator.Generar(new CufGeneracionRequest(
                    Nit: _siat.Nit,
                    FechaEmision: fechaEmisionCuf,
                    CodigoSucursal: pvActual.CodigoSucursal,
                    CodigoModalidad: _siat.CodigoModalidad,
                    TipoEmision: tipoEmisionContingencia,
                    TipoFacturaDocumento: _siat.TipoFacturaDocumento,
                    CodigoDocumentoSector: _siat.CodigoDocumentoSector,
                    NumeroFactura: numeroFactura,
                    CodigoPuntoVenta: pvActual.CodigoPuntoVenta,
                    CodigoControl: codigoControl));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "CUF no generado en modo contingencia para NumeroFactura={N}",
                    numeroFactura);
                throw new VentaException(
                    $"No se pudo generar el CUF en modo contingencia. Detalle: {ex.Message}");
            }

            var venta = CrearVentaBase(
                datos, cajero, cliente, numeroDocumento, fechaEmisionParaBd,
                totalCobrar, descuento, detallesVenta);

            venta.CodigoSucursal = pvActual.CodigoSucursal;
            venta.CodigoPuntoVenta = pvActual.CodigoPuntoVenta;

            venta.Facturado = true;
            venta.NumeroFactura = numeroFactura;
            venta.Cuf = cuf;
            venta.Cufd = cufdCodigo;
            venta.TipoEmision = tipoEmisionContingencia;
            venta.EventoSignificativoSiatId = eventoId;
            venta.CodigoTipoDocumentoIdentidad = datos.CodigoTipoDocumento!.Value;

            // Leyenda: la contingencia no es motivo para saltarse la obligación.
            venta.Leyenda = await catLeyendaResolver.ObtenerAleatoriaAsync(actividadEconomica, default);

            // Pendiente hasta que el monitor detecte recuperación y reenvíe la factura.
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
                logger.LogError(ex, "XML/archivo/hash de factura offline no generado");
                throw new InventarioException(
                    "No se pudo generar el archivo de factura en modo contingencia.");
            }

            return venta;
        }

        private async Task<(decimal Total, ResultadoAplicacionDescuentoPromocion? Descuento)> ResolverTotalCobrarAsync(
            DtoVentaPedido datos,
            Cliente cliente,
            decimal subtotal,
            string codigoVenta)
        {
            if (!datos.AplicarDescuentos)
                return (subtotal, null);

            var descuento = await _promocionDescuento.AplicarDescuentoAsync(cliente, subtotal, codigoVenta);
            if (descuento is null)
                throw new InventarioException("No hay descuentos aplicables para este pedido y cliente.");

            return (descuento.TotalConDescuento, descuento);
        }

        private Venta ConstruirVentaSinFactura(
            DtoVentaPedido datos,
            string cajero,
            Cliente cliente,
            string numeroDocumento,
            DateTime fechaEmision,
            string codigoVenta,
            decimal totalCobrar,
            ResultadoAplicacionDescuentoPromocion? descuento,
            List<Detalle_Pago> detallesVenta)
        {
            var venta = CrearVentaBase(
                datos, cajero, cliente, numeroDocumento, fechaEmision, totalCobrar, descuento, detallesVenta);
            venta.Facturado = false;
            venta.NumeroFactura = null;
            venta.Cuf = $"NF-{codigoVenta}";
            venta.Cufd = "N/A";
            venta.CodigoTipoDocumentoIdentidad = datos.CodigoTipoDocumento ?? 1;
            venta.Leyenda = "Venta sin factura electrónica";
            venta.EstadoSiat = null;
            venta.XmlBase64 = null;
            venta.CodigoHash = null;
            venta.CodigoRecepcion = null;
            venta.ErrorMensaje = null;
            return venta;
        }

        private Venta CrearVentaBase(
            DtoVentaPedido datos,
            string cajero,
            Cliente cliente,
            string numeroDocumento,
            DateTime fechaEmision,
            decimal totalCobrar,
            ResultadoAplicacionDescuentoPromocion? descuento,
            List<Detalle_Pago> detallesVenta)
        {
            // Construir las líneas de pago (VentaPagos) y resolver el código
            // principal que se serializa en el XML (el de mayor monto).
            var (lineasPago, codigoPrincipal) = ResolverLineasYPagoPrincipal(datos.Pagos);

            return new Venta
            {
                NitEmisor = _siat.Nit,
                RazonSocialEmisor = _empresa.RazonSocial,
                Municipio = _empresa.Municipio,
                Telefono = string.IsNullOrWhiteSpace(_empresa.Telefono) ? null : _empresa.Telefono.Trim(),
                CodigoSucursal = _siat.CodigoSucursal,
                CodigoPuntoVenta = _siat.CodigoPuntoVenta,
                Direccion = _empresa.Direccion,
                FechaEmision = fechaEmision,
                NumeroDocumento = numeroDocumento,
                Complemento = ResolverComplemento(datos),
                CodigoCliente = ResolverCodigoCliente(cliente),
                CodigoMetodoPago = codigoPrincipal,
                NumeroTarjeta = null,
                MontoGiftCard = null,
                MontoTotal = totalCobrar,
                MontoTotalSujetoIva = totalCobrar,
                DescuentoAdicional = descuento?.MontoDescuento > 0 ? descuento.MontoDescuento : null,
                CodigoExcepcion = _siat.CodigoExcepcion,
                Cafc = string.IsNullOrWhiteSpace(_siat.Cafc) ? null : _siat.Cafc.Trim(),
                CodigoMoneda = _siat.CodigoMoneda,
                TipoCambio = _siat.TipoCambio,
                MontoTotalMoneda = totalCobrar,
                Usuario = cajero,
                CodigoDocumentoSector = _siat.CodigoDocumentoSector,
                TipoEmision = _siat.CodigoEmision,
                NombreRazonSocial = string.IsNullOrWhiteSpace(cliente.Nombre)
                    ? null
                    : cliente.Nombre.Trim(),
                Detalles = detallesVenta,
                Pagos = lineasPago,
                Cuf = string.Empty,
                Cufd = string.Empty,
                Leyenda = string.Empty
            };
        }

        /// <summary>
        /// Toma la lista de pagos del DTO y arma:
        ///   1. Las entidades <c>VentaPago</c> (1 fila por método con monto > 0).
        ///   2. El código de método de pago PRINCIPAL que va en el XML al SIAT.
        ///      Hoy el XSD SIAT acepta un solo <c>codigoMetodoPago</c> por factura,
        ///      así que se toma la línea de MAYOR monto (la más representativa).
        ///
        /// Nota: la validación contra <c>MetodoPagoSiatCatalogo</c> la hace
        /// <c>DtoPagos.Validate</c> en el request — si llega acá significa que
        /// todos los códigos son válidos y activos.
        /// </summary>
        private static (List<VentaPago> Lineas, int CodigoPrincipal) ResolverLineasYPagoPrincipal(
            DtoPagos pagos)
        {
            var lineasConMonto = (pagos.Lineas ?? new List<DtoPagoLinea>())
                .Where(l => l.Monto > 0)
                .ToList();

            if (lineasConMonto.Count == 0)
            {
                // No debería pasar porque DtoPagos.Validate ya garantiza Total > 0.
                return (new List<VentaPago>(), (int)TipoPagos.Efectivo);
            }

            var entidades = lineasConMonto
                .Select(l => new VentaPago
                {
                    CodigoMetodoPago = l.CodigoMetodoPago,
                    Monto = l.Monto
                })
                .ToList();

            // Pago principal = el de mayor monto. Si hay empate, el primero de la lista.
            var principal = lineasConMonto
                .OrderByDescending(l => l.Monto)
                .First()
                .CodigoMetodoPago;

            return (entidades, principal);
        }

        private (List<Detalle_Pago> Detalles, bool TieneCombo) ConstruirDetalles(
            Pedido pedido,
            bool validarUnidadSiat,
            string actividadEconomica)
        {
            var detallesVenta = new List<Detalle_Pago>();
            var tieneCombo = false;

            // Diccionario de consolidación: si el mismo producto aparece en varias
            // rondas (ej: 2x Café en ronda 1, 3x Café en ronda 2), lo agrupamos en
            // una sola línea de Detalle_Pago sumando Cantidad y SubTotal. Clave:
            // CodigoProducto (más estable que el nombre). Fallback al nombre
            // normalizado para productos sin código resuelto.
            var detallesPorProducto = new Dictionary<string, Detalle_Pago>(StringComparer.Ordinal);

            foreach (var ronda in pedido.Rondas)
            {
                foreach (var detalle in ronda.Detalle)
                {
                    if (detalle.ItemsCombo.Count > 0)
                        tieneCombo = true;

                    var codigo = ResolverCodigoProducto(detalle);
                    var key = !string.IsNullOrWhiteSpace(codigo)
                        ? $"cod:{codigo}"
                        : $"nom:{detalle.Nombre_Producto.Trim().ToLowerInvariant()}";

                    var subtotalLinea = detalle.Precio * detalle.Cantidad;

                    if (detallesPorProducto.TryGetValue(key, out var existente))
                    {
                        // Sumar Cantidad y SubTotal; mantener el resto del primer
                        // registro (descripción, precio unitario, código SIN, etc.).
                        existente.Cantidad += detalle.Cantidad;
                        existente.SubTotal += subtotalLinea;
                    }
                    else
                    {
                        detallesPorProducto[key] = new Detalle_Pago
                        {
                            ActividadEconomica = actividadEconomica,
                            CodigoProductoSin = ResolverCodigoProductoSin(detalle),
                            CodigoProducto = codigo,
                            Descripcion = detalle.Nombre_Producto,
                            Cantidad = detalle.Cantidad,
                            UnidadMedida = validarUnidadSiat
                                ? ResolverUnidadMedidaSiat(detalle)
                                : ResolverUnidadMedidaInterna(detalle),
                            PrecioUnitario = detalle.Precio,
                            MontoDescuento = null,
                            SubTotal = subtotalLinea,
                            NumeroSerie = null,
                            NumeroImei = null
                        };
                    }
                }
            }

            detallesVenta.AddRange(detallesPorProducto.Values);

            return (detallesVenta, tieneCombo);
        }

        private static int ResolverUnidadMedidaInterna(Detalle_ronda detalle)
        {
            if (detalle.CodigoUnidadMedida > 0)
                return detalle.CodigoUnidadMedida;

            return 58;
        }

        private static int ResolverUnidadMedidaSiat(Detalle_ronda detalle)
        {
            var codigo = detalle.CodigoUnidadMedida;

            if (codigo <= 0)
            {
                throw new VentaException(
                    $"El producto '{detalle.Nombre_Producto}' no tiene unidad de medida SIAT configurada.");
            }

            if (!UnidadMedidaSiatService.EsCodigoValido(codigo))
            {
                throw new VentaException(
                    $"El producto '{detalle.Nombre_Producto}' tiene un código de unidad de medida SIAT no válido ({codigo}).");
            }

            return codigo;
        }

        private static int ResolverCodigoProductoSin(Detalle_ronda detalle)
        {
            if (!string.IsNullOrWhiteSpace(detalle.CodigoSin)
                && int.TryParse(detalle.CodigoSin.Trim(), out var codigo))
                return codigo;

            if (detalle.Producto is not null
                && !string.IsNullOrWhiteSpace(detalle.Producto.CodigoSin)
                && int.TryParse(detalle.Producto.CodigoSin.Trim(), out var legacy))
                return legacy;

            throw new VentaException(
                $"El producto '{detalle.Nombre_Producto}' no tiene código SIN configurado. "
                + "Configure el código SIN en el producto antes de facturar.");
        }

        /// <summary>
        /// Resuelve el (sucursal, puntoVenta) que se usará para este cobro.
        ///
        /// Orden de prioridad:
        /// 1. Si el frontend envió CodigoSucursal + CodigoPuntoVenta en el DTO
        ///    (vía selector del navbar), se valida contra BD que exista y esté
        ///    activo. Si no cumple, lanza VentaException claro. Si cumple, se usa.
        /// 2. Si NO vino del frontend, se cae al comportamiento legacy
        ///    (<see cref="ResolverPuntoVentaActivo"/>): primer PuntosVentaSiat
        ///    activo ordenado por (Suc, PV), o appsettings si no hay ninguno.
        ///
        /// Esto garantiza que cuando hay varios PVs activos el cobro use
        /// exactamente el PV que el cajero seleccionó, y que CUF/CUFD/sobre
        /// SOAP estén alineados (mismo Suc y mismo PV).
        /// </summary>
        private async Task<(int CodigoSucursal, int CodigoPuntoVenta)> ResolverPuntoVentaParaCobroAsync(
            DtoVentaPedido datos)
        {
            if (datos.CodigoSucursal is int sucFront && datos.CodigoPuntoVenta is int pvFront)
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var pvBd = await db.PuntosVentaSiat
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.CodigoSucursal == sucFront
                        && p.CodigoPuntoVenta == pvFront
                        && p.Activo);

                if (pvBd is null)
                {
                    throw new VentaException(
                        $"El punto de venta (Suc={sucFront}, PV={pvFront}) no existe "
                        + "o está inactivo. Verifica la configuración de PuntosVentaSiat "
                        + "o selecciona otra sucursal desde el navbar.");
                }

                logger.LogInformation(
                    "PV seleccionado por frontend para este cobro: (Suc={Suc}, PV={PV}) '{Nombre}'",
                    sucFront, pvFront, pvBd.Nombre);
                return (sucFront, pvFront);
            }

            // Fallback: comportamiento legacy (sin breaking change).
            return ResolverPuntoVentaActivo();
        }

        /// <summary>
        /// Resuelve el (sucursal, puntoVenta) activo desde la tabla PuntosVentaSiat.
        ///
        /// Es el fallback de <see cref="ResolverPuntoVentaParaCobroAsync"/>
        /// cuando el frontend NO envía codigoSucursal/codigoPuntoVenta en el body
        /// del cobro. El camino normal es que el selector del header del frontend
        /// (que persiste en localStorage) envíe el PV elegido, pero mantenemos
        /// este fallback para que cobros sin body de PV sigan funcionando.
        ///
        /// Comportamiento:
        ///   - Si hay 1 PV activo → lo usa.
        ///   - Si hay 0 PV activos → cae a appsettings (SiatOptions) como
        ///     último fallback y loguea warning.
        ///   - Si hay MÁS DE 1 PV activo → lanza VentaException claro pidiendo
        ///     que el cajero use el selector del header. NO elige uno
        ///     silenciosamente porque reproduciría el bug 1002/1008 que ya
        ///     arreglamos (el cajero nunca sabría qué PV se está usando).
        /// </summary>
        private (int CodigoSucursal, int CodigoPuntoVenta) ResolverPuntoVentaActivo()
        {
            using var db = dbFactory.CreateDbContext();
            var pvs = db.PuntosVentaSiat
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.CodigoSucursal)
                .ThenBy(p => p.CodigoPuntoVenta)
                .ToList();

            if (pvs.Count == 1)
                return (pvs[0].CodigoSucursal, pvs[0].CodigoPuntoVenta);

            if (pvs.Count == 0)
            {
                logger.LogWarning(
                    "No hay PuntosVentaSiat activos. Usando fallback de appsettings: ({Suc},{PV}). "
                    + "Active al menos UN PV en la tabla PuntosVentaSiat o usa el selector del header.",
                    _siat.CodigoSucursal, _siat.CodigoPuntoVenta);
                return (_siat.CodigoSucursal, _siat.CodigoPuntoVenta);
            }

            // Más de uno activo: NO elegir silenciosamente. Pedirle al cajero
            // que use el selector del header (que sí envía el PV en el body del cobro).
            var candidatos = string.Join(", ",
                pvs.Select(p => $"(Suc={p.CodigoSucursal}, PV={p.CodigoPuntoVenta}) '{p.Nombre}'"));

            throw new VentaException(
                "Tienes " + pvs.Count + " PuntosVenta activos: " + candidatos
                + ". Para cobrar tenés que elegir uno con el selector que está en el header "
                + "(arriba a la derecha). Si los datos de PuntosVentaSiat están mal configurados, "
                + "corrige la tabla o desactiva los que no uses: "
                + "UPDATE \"PuntosVentaSiat\" SET \"Activo\" = false WHERE \"CodigoSucursal\" = S "
                + "AND \"CodigoPuntoVenta\" = P;");
        }

        /// <summary>
        /// (Eliminado — la lógica vive ahora en <see cref="ICatActividadResolver"/>
        /// y se invoca una sola vez al inicio de <see cref="ProcesarVenta"/>.
        /// El CAEB se pasa a <see cref="ConstruirDetalles"/> como parámetro.)
        /// </summary>

        private static string ResolverCodigoProducto(Detalle_ronda detalle)
        {
            if (!string.IsNullOrWhiteSpace(detalle.Codigo))
                return detalle.Codigo.Trim();

            if (!string.IsNullOrWhiteSpace(detalle.Producto?.Codigo))
                return detalle.Producto.Codigo.Trim();

            return ProductoCodigoService.Generar(detalle.Id_Producto);
        }

        private static string? ResolverComplemento(DtoVentaPedido datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Complemento))
                return null;

            return datos.Complemento.Trim();
        }

        private static string ResolverCodigoCliente(Cliente cliente)
        {
            if (!string.IsNullOrWhiteSpace(cliente.Codigo))
                return cliente.Codigo;

            return ClienteCodigoService.Generar(cliente.Nombre, cliente.Id);
        }

        /// <summary>
        /// Pieza 4 — helper del catch reactivo en <see cref="ConstruirVentaFacturadaAsync"/>.
        /// Re-consulta BD por si el monitor (Piezas 1-3) ya persistió contingencia
        /// y, si no, intenta forzarla vía <see cref="IEventoSignificativoSiatService.RegistrarLocalmenteSinSoapAsync"/>
        /// directamente desde el flujo de venta.
        ///
        /// Cubre el caso "primer cobro con SIAT caído" donde el monitor todavía no
        /// cruzó el umbral (FallosConsecutivos=1 &lt; Umbral=2) y por lo tanto
        /// DispararContingenciaAutomaticaAsync nunca se ejecutó.
        ///
        /// Si después de forzar el registro tampoco hay contingencia activa, devuelve
        /// null y el catch del cobro propaga el error original al operador.
        /// </summary>
        private async Task<EstadoContingenciaDto?> IntentarActivarContingenciaReactivaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct)
        {
            // 1) Re-consultar BD por si el monitor (Pieza 2) ya persistió
            //    contingencia en respuesta a este mismo fallo o uno previo.
            var contingenciaExistente = await _eventoSignificativoSiat
                .ObtenerEstadoContingenciaAsync(codigoSucursal, codigoPuntoVenta, ct);

            if (contingenciaExistente.ContingenciaActiva)
            {
                logger.LogInformation(
                    "Pieza 4 — contingencia ya activa en BD (monitor pudo haberla creado). "
                  + "Reutilizando EventoId={Id}.",
                    contingenciaExistente.EventoSignificativoId);
                return contingenciaExistente;
            }

            // 2) No hay contingencia activa. Forzar registro local (Pieza 1)
            //    directamente desde el flujo de venta. Caso típico: primer cobro
            //    con SIAT caído, monitor todavía en FallosConsecutivos=1 < 2.
            try
            {
                // motivo=1 = CORTE DEL SERVICIO DE INTERNET (hardcoded porque
                // este es el motivo por defecto del monitor; ver DetectorOptions).
                // Si en el futuro se quiere parametrizar, leer de appsettings.
                var resultado = await _eventoSignificativoSiat.RegistrarLocalmenteSinSoapAsync(
                    motivo: 1,
                    origen: "AutomaticoSinSoap",
                    codigoSucursal: codigoSucursal,
                    codigoPuntoVenta: codigoPuntoVenta,
                    descripcion:
                        "FALLBACK REACTIVO: SIAT caído detectado durante cobro de venta.",
                    ct: ct);

                logger.LogInformation(
                    "Pieza 4 — contingencia reactiva registrada desde flujo de venta. "
                  + "EventoId={Id}, Suc={Suc}, PV={PV}",
                    resultado.EventoId, codigoSucursal, codigoPuntoVenta);

                // Re-consultar BD para obtener el EstadoContingenciaDto completo.
                return await _eventoSignificativoSiat
                    .ObtenerEstadoContingenciaAsync(codigoSucursal, codigoPuntoVenta, ct);
            }
            catch (VentaException vex) when (
                vex.Message.Contains("Ya existe una contingencia activa"))
            {
                // Race: el monitor (Pieza 2) u otro thread ganó la carrera.
                // Re-consultar BD para usar la contingencia que sí quedó.
                var contingenciaRace = await _eventoSignificativoSiat
                    .ObtenerEstadoContingenciaAsync(codigoSucursal, codigoPuntoVenta, ct);

                if (contingenciaRace.ContingenciaActiva)
                {
                    logger.LogInformation(
                        "Pieza 4 — race con monitor: contingencia activa detectada. "
                      + "EventoId={Id}.",
                        contingenciaRace.EventoSignificativoId);
                    return contingenciaRace;
                }

                logger.LogWarning(
                    "Pieza 4 — race detectada pero BD no muestra contingencia activa. "
                  + "Cajero verá error original.");
                return null;
            }
            catch (Exception fallbackEx)
            {
                logger.LogWarning(fallbackEx,
                    "Pieza 4 — fallback reactivo no pudo registrar contingencia. "
                  + "Cajero verá error original.");
                return null;
            }
        }
    }
}
