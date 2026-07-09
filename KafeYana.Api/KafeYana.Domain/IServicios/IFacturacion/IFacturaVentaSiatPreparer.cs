using KafeYana.Domain.Entities;



namespace KafeYana.Application.IServicios.IFacturacion

{

    public interface IFacturaVentaSiatPreparer

    {

        /// <summary>

        /// Asigna correlativo SIAT, CUF/CUFD, leyenda, XML y hash a una venta cobrada sin factura.

        /// </summary>

        Task PrepararVentaSinFacturarAsync(Venta venta, CancellationToken ct = default);

        /// <summary>
        /// Regenera CUF/CUFD/XML/Hash de una venta ya Facturada pero Observada/Pendiente
        /// (el SIAT nunca la validó), reusando el NumeroFactura ya asignado. Se usa tras
        /// corregir datos fiscales erróneos antes de reenviar.
        /// </summary>
        Task RegenerarVentaObservadaAsync(Venta venta, CancellationToken ct = default);

    }

}


