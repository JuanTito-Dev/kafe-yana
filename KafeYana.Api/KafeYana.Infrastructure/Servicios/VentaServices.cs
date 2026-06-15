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

            var cliente = pedido.Cliente;
            if (pedido.Id_Cliente is null || pedido.Id_Cliente != datos.Id_Cliente)
            {
                cliente = await _db.clientes.FindByIdAsync(datos.Id_Cliente);
                if (cliente is null)
                    throw new InventarioException("Cliente no encontrado.");
            }

            if (!cliente.Estado)
                throw new VentaException("El cliente está inactivo y no puede realizarse el cobro.");

            var fechaEmision = SiatFechaEmision.AhoraUtc();
            var anio = fechaEmision.Year;
            var numeroFactura = await _db.ventas.SiguienteNumeroVentaAsync();
            var codigoVenta = $"VTA-{anio}-{numeroFactura:D3}";

            await _inventarioPedidoCompromiso.AplicarMovimientosYCerrarAsync(datos.Id_Pedido, codigoVenta);

            // Lista DETALLE SIAT: un ítem por producto en todas las rondas del pedido.
            var detallesVenta = new List<Detalle_Pago>();
            var tieneCombo = false;

            foreach (var ronda in pedido.Rondas)
            {
                foreach (var detalle in ronda.Detalle)
                {
                    detallesVenta.Add(new Detalle_Pago
                    {
                        ActividadEconomica = _empresa.CodigoActividad,
                        CodigoProductoSin = ResolverCodigoProductoSin(detalle),
                        CodigoProducto = ResolverCodigoProducto(detalle),
                        Descripcion = detalle.Nombre_Producto,
                        Cantidad = detalle.Cantidad,
                        UnidadMedida = ResolverUnidadMedidaSiat(detalle),
                        PrecioUnitario = detalle.Precio,
                        MontoDescuento = null,
                        SubTotal = detalle.Precio * detalle.Cantidad,
                        NumeroSerie = null,
                        NumeroImei = null
                    });

                    if (detalle.ItemsCombo.Count > 0)
                        tieneCombo = true;
                }
            }

            if (detallesVenta.Count == 0)
                throw new VentaException("No se pudo armar el detalle de la factura. Verifique los productos del pedido.");

            var subtotal = pedido.Rondas.Sum(r => r.SubTotal);

            if (subtotal <= 0)
                throw new VentaException("El total del pedido debe ser mayor a cero.");

            ResultadoAplicacionDescuentoPromocion? descuento = null;

            var totalCobrar = subtotal;

            if (datos.AplicarDescuentos)
            {
                descuento = await _promocionDescuento.AplicarDescuentoAsync(cliente!, subtotal, codigoVenta);
                if (descuento is null)
                    throw new InventarioException("No hay descuentos aplicables para este pedido y cliente.");

                totalCobrar = descuento.TotalConDescuento;
            }

            if (datos.Pagos.Total != totalCobrar)
            {
                var esperado = datos.AplicarDescuentos
                    ? $"total con descuento ({totalCobrar:F2})"
                    : $"total del pedido ({totalCobrar:F2})";

                throw new InventarioException($"El total de los pagos no coincide con el {esperado}.");
            }

            var cuf = $"PENDIENTE-{codigoVenta}";
            var cufdCodigo = "PENDIENTE";

            // ── CUF + CUFD ──────────────────────────────────────────────────────
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
                logger.LogWarning(ex, "CUF/CUFD no generado; se guarda PENDIENTE");
            }

            var venta = new Venta
            {
                NitEmisor = _siat.Nit,
                RazonSocialEmisor = _empresa.RazonSocial,
                Municipio = _empresa.Municipio,
                Telefono = string.IsNullOrWhiteSpace(_empresa.Telefono) ? null : _empresa.Telefono.Trim(),
                NumeroFactura = numeroFactura,
                Cuf = cuf,
                Cufd = cufdCodigo,
                CodigoSucursal = _siat.CodigoSucursal,
                CodigoPuntoVenta = _siat.CodigoPuntoVenta,
                Direccion = _empresa.Direccion,
                FechaEmision = fechaEmision,
                CodigoTipoDocumentoIdentidad = datos.CodigoTipoDocumento,
                NumeroDocumento = datos.NumeroDocumento.Trim(),
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
                Leyenda = LeyendaSiatService.ObtenerAleatoria(),
                Usuario = cajero,
                CodigoDocumentoSector = _siat.CodigoDocumentoSector,
                TipoEmision = _siat.CodigoEmision,
                NombreRazonSocial = string.IsNullOrWhiteSpace(cliente.Nombre)
                    ? null
                    : cliente.Nombre.Trim(),
                Detalles = detallesVenta,
                EstadoSiat = FacturaEstado.Pendiente
            };

            // ── XML → GZIP → Base64 → hash (envío SIAT después de guardar) ──
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
                throw new InventarioException("No se pudo generar el archivo de factura para guardar en la venta.");
            }

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

            if (detalle.Producto is null || string.IsNullOrWhiteSpace(detalle.Producto.CodigoSin))
                return 0;

            return int.TryParse(detalle.Producto.CodigoSin.Trim(), out var legacy) ? legacy : 0;
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
