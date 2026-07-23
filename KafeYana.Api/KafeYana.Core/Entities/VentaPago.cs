namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Línea de pago individual de una venta. Permite registrar pagos mixtos
    /// (ej: 50 Bs efectivo + 30 Bs QR) sin perder la granularidad por método.
    ///
    /// Hoy KafeYana emite al SIAT un solo <c>codigoMetodoPago</c> por venta
    /// (el de mayor monto), por lo que esta tabla es principalmente para
    /// auditoría y para futuras implementaciones de pagos mixtos en el XML.
    /// </summary>
    public class VentaPago
    {
        public int Id { get; set; }

        public int IdVenta { get; set; }

        /// <summary>Código SIN del método de pago (FK lógica a CatMetodosPago.Codigo).</summary>
        public int CodigoMetodoPago { get; set; }

        /// <summary>Monto pagado con este método (en Bolivianos).</summary>
        public decimal Monto { get; set; }
    }
}