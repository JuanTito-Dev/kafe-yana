using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
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
        IOptions<SiatOptions> siatOpts,
        IOptions<DatosEmpresaOptions> empresaOpts,
        ILogger<VentaServices> logger) : IVentaServices
    {
        private readonly SiatOptions _siat = siatOpts.Value;
        private readonly DatosEmpresaOptions _empresa = empresaOpts.Value;

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

            var fechaEmision = SiatFechaEmision.AhoraUtc();
            var anio = fechaEmision.Year;
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

            var (detallesVenta, tieneCombo) = ConstruirDetalles(pedido, validarUnidadSiat: datos.Factura);
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
                ? await ConstruirVentaFacturadaAsync(datos, cajero, cliente, numeroDocumento, fechaEmision, numeroFacturaSiat, totalCobrar, descuento, detallesVenta)
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
            List<Detalle_Pago> detallesVenta)
        {
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
                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    _siat.CodigoSucursal,
                    _siat.CodigoPuntoVenta);

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
                logger.LogError(
                    ex,
                    "CUF/CUFD no generado al facturar número {NumeroFactura}; abortando cobro para no persistir PENDIENTE",
                    numeroFactura);
                throw new VentaException(
                    "No se pudo generar el CUF/CUFD para la factura. "
                    + "El CUFD puede haber vencido o el SIAT no responde. Intente nuevamente.");
            }

            var venta = CrearVentaBase(
                datos, cajero, cliente, numeroDocumento, fechaEmision, totalCobrar, descuento, detallesVenta);
            venta.Facturado = true;
            venta.NumeroFactura = numeroFactura;
            venta.Cuf = cuf;
            venta.Cufd = cufdCodigo;
            venta.CodigoTipoDocumentoIdentidad = datos.CodigoTipoDocumento!.Value;
            venta.Leyenda = LeyendaSiatService.ObtenerAleatoria();
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
                CodigoMetodoPago = ResolverMetodoPago(datos.Pagos),
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
                Cuf = string.Empty,
                Cufd = string.Empty,
                Leyenda = string.Empty
            };
        }

        private (List<Detalle_Pago> Detalles, bool TieneCombo) ConstruirDetalles(
            Pedido pedido,
            bool validarUnidadSiat)
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
                            ActividadEconomica = _empresa.CodigoActividad,
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

        private static int ResolverMetodoPago(DtoPagos pagos)
        {
            var metodosUsados = 0;
            if (pagos.Efectivo > 0) metodosUsados++;
            if (pagos.Tarjeta > 0) metodosUsados++;
            if (pagos.Qr > 0) metodosUsados++;

            if (metodosUsados != 1)
                return (int)TipoPagos.Otros;

            if (pagos.Efectivo > 0) return (int)TipoPagos.Efectivo;
            if (pagos.Tarjeta > 0) return (int)TipoPagos.Tarjeta;
            return (int)TipoPagos.Qr;
        }
    }
}
