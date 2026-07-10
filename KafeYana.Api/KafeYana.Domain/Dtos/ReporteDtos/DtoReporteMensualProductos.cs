namespace KafeYana.Application.Dtos.ReporteDtos
{
    /// <summary>
    /// Reporte mensual de productos más vendidos / con mayor rotación.
    /// Fuente: <c>Venta.Detalles</c> filtrada por mes calendario en TZ La Paz,
    /// sólo ventas válidas/observadas (no anuladas ni pendientes).
    /// </summary>
    public class DtoReporteMensualProductos
    {
        public int Mes { get; set; }

        public int Anio { get; set; }

        public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;

        public DtoResumenMensualProductos Resumen { get; set; } = new();

        /// <summary>Top 10 productos por unidades vendidas (descendente).</summary>
        public List<DtoProductoTop> TopUnidades { get; set; } = new();

        /// <summary>Top 10 productos por ingresos generados (descendente).</summary>
        public List<DtoProductoTop> TopIngresos { get; set; } = new();

        /// <summary>Top 10 productos por rotación (descendente).</summary>
        public List<DtoProductoTop> TopRotacion { get; set; } = new();

        /// <summary>Totales agregados por categoría de producto.</summary>
        public List<DtoProductoPorCategoria> PorCategoria { get; set; } = new();
    }

    public class DtoResumenMensualProductos
    {
        /// <summary>Suma de MontoTotal de las ventas del mes.</summary>
        public decimal TotalFacturado { get; set; }

        public int NumeroFacturas { get; set; }

        public int UnidadesVendidas { get; set; }

        /// <summary>Cantidad de productos distintos vendidos en el mes.</summary>
        public int ProductosDistintos { get; set; }

        public int CategoriasActivas { get; set; }
    }

    public class DtoProductoTop
    {
        public int IdProducto { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public int UnidadesVendidas { get; set; }

        public decimal Ingresos { get; set; }

        public decimal PrecioPromedio { get; set; }

        public int NumeroVentas { get; set; }

        /// <summary>Snapshot de stock al cierre del mes (espejo del stock actual si no hubo migraciones después).</summary>
        public decimal StockFinMes { get; set; }

        /// <summary>Stock reconstruido al inicio del mes; 0 si no hay datos.</summary>
        public decimal StockInicioMes { get; set; }

        /// <summary>Promedio (StockInicioMes + StockFinMes) / 2; usado como denominador de Rotación.</summary>
        public decimal StockPromedio { get; set; }

        /// <summary>UnidadesVendidas / StockPromedio. 0 si StockPromedio == 0.</summary>
        public decimal Rotacion { get; set; }
    }

    public class DtoProductoPorCategoria
    {
        public string Categoria { get; set; } = string.Empty;

        public int UnidadesVendidas { get; set; }

        public decimal Ingresos { get; set; }

        public int ProductosDistintos { get; set; }

        public List<DtoProductoTop> Productos { get; set; } = new();
    }
}
