namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// KPIs agregados de ventas devueltos por <c>ventasEstadisticas</c> en GraphQL.
    /// Los totales respetan el mismo filtro <c>where</c> que la lista de ventas y se
    /// calculan en el backend sobre TODAS las páginas coincidentes (no sólo la
    /// primera página devuelta al cliente).
    ///
    /// "Hoy" y "Mes" se evalúan en la zona horaria del servidor (UTC si no se puede
    /// resolver America/La_Paz); las fechas en <c>Venta.FechaEmision</c> están en UTC.
    /// </summary>
    public class VentasEstadisticas
    {
        /// <summary>Suma de <c>MontoTotal</c> de ventas del día actual (status = completed, no anulada).</summary>
        public decimal TotalHoy { get; set; }

        /// <summary>Suma de <c>MontoTotal</c> de ventas del mes actual (status = completed, no anulada).</summary>
        public decimal TotalMes { get; set; }

        /// <summary>Cantidad de ventas del día actual (status = completed).</summary>
        public int ConteoHoy { get; set; }

        /// <summary>Cantidad de ventas del mes actual (status = completed).</summary>
        public int ConteoMes { get; set; }

        /// <summary>Ticket promedio del mes actual = <c>TotalMes / ConteoMes</c>. 0 si no hay ventas.</summary>
        public decimal TicketPromedioMes { get; set; }
    }
}
