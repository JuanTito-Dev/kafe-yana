using KafeYana.Application.Exceptions;

using KafeYana.Application.IRepositorio;

using KafeYana.Application.IServicios.IFacturacion;

using KafeYana.Domain.Entities;

using KafeYana.Domain.TiposDeDatos;

using KafeYana.Infrastructure.Configuration;

using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;



namespace KafeYana.Infrastructure.Servicios.Facturacion

{

    public class FacturaVentaSiatPreparer(

        IUnitWork _db,

        IRecepcionFacturaService _recepcionFactura,

        IFacturaXmlGenerator _facturaXmlGenerator,

        ICufdService _cufdService,

        ICufGenerator _cufGenerator,

        IOptions<SiatOptions> siatOpts,

        ILogger<FacturaVentaSiatPreparer> logger) : IFacturaVentaSiatPreparer

    {

        private readonly SiatOptions _siat = siatOpts.Value;



        public async Task PrepararVentaSinFacturarAsync(Venta venta, CancellationToken ct = default)

        {

            if (venta.Facturado)

                throw new VentaException("La venta ya está marcada como facturada.");



            if (venta.Detalles.Count == 0)

                throw new VentaException("La venta no tiene detalle para generar la factura.");



            ValidarDetallesSiat(venta.Detalles);



            var numeroFactura = await _db.ventas.SiguienteNumeroFacturaSiatAsync();

            var fechaEmision = SiatFechaEmision.AhoraUtc();

            var cuf = $"PENDIENTE-VTA-{fechaEmision.Year}-{numeroFactura:D3}";

            var cufdCodigo = "PENDIENTE";



            try

            {

                var cufd = await _cufdService.ObtenerCufdVigenteAsync(

                    _siat.CodigoSucursal,

                    _siat.CodigoPuntoVenta,

                    ct);



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

                logger.LogWarning(ex, "CUF/CUFD no generado al facturar venta {VentaId}; se guarda PENDIENTE", venta.Id);

            }



            venta.Facturado = true;

            venta.NumeroFactura = numeroFactura;

            venta.FechaEmision = fechaEmision;

            venta.Cuf = cuf;

            venta.Cufd = cufdCodigo;

            venta.Leyenda = LeyendaSiatService.ObtenerAleatoria();

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

                logger.LogError(ex, "XML/archivo/hash no generado al facturar venta {VentaId}", venta.Id);

                throw new VentaException("No se pudo generar el archivo de factura para enviar al SIAT.");

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

    }

}


