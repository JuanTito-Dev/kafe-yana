using KafeYana.Domain.Entities;



namespace KafeYana.Application.IServicios.IFacturacion

{

    public interface IFacturaVentaSiatPreparer

    {

        /// <summary>

        /// Asigna correlativo SIAT, CUF/CUFD, leyenda, XML y hash a una venta cobrada sin factura.

        /// </summary>

        Task PrepararVentaSinFacturarAsync(Venta venta, CancellationToken ct = default);

    }

}


