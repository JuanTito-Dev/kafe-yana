namespace KafeYana.Application.Dtos.ImpresoraDtos
{
    public class ItemImprimirDto
    {
        public int Cantidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Nota { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? Precio { get; set; }
        public decimal? Total { get; set; }
    }
}
