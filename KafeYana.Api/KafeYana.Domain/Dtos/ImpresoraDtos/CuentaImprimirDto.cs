namespace KafeYana.Application.Dtos.ImpresoraDtos
{
    public class CuentaImprimirDto
    {
        public string Mesa { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public List<ItemImprimirDto> Items { get; set; } = [];
        public decimal Total { get; set; }
        public string? MetodoPago { get; set; }
        public List<string> Destinos { get; set; } = ["principal"];
    }
}
