namespace KafeYana.Application.Dtos.ReporteDtos
{
    public class DtoReporteInventario
    {
        public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;

        public DtoResumenInventario Resumen { get; set; } = new();

        public List<DtoCategoriaProdutos> DistribucionPorCategoria { get; set; } = new();

        public List<DtoItemStockBajo> StockBajoCritico { get; set; } = new();

        public List<DtoItemStockBajo> ProximosAgotarse { get; set; } = new();
    }

    public class DtoResumenInventario
    {
        public int TotalProductos { get; set; }
        public int TotalInsumos { get; set; }
        public int ItemsConStockBajo { get; set; }
        public decimal ValorInventario { get; set; }
    }

    public class DtoCategoriaProdutos
    {
        public string Categoria { get; set; } = string.Empty;
        public int TotalProductos { get; set; }
    }

    public class DtoItemStockBajo
    {
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Stock { get; set; } = string.Empty;
        public string Minimo { get; set; } = string.Empty;

        /// <summary>Porcentaje de stock actual respecto al mínimo. 0% = crítico.</summary>
        public int Ratio { get; set; }
    }
}
