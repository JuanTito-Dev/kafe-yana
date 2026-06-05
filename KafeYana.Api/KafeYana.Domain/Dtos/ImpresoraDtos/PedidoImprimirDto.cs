namespace KafeYana.Application.Dtos.ImpresoraDtos
{
    public class PedidoImprimirDto
    {
        public string Mesa { get; set; } = string.Empty;
        public string? Ronda { get; set; }
        public List<ItemImprimirDto> Items { get; set; } = [];
        public List<string> Destinos { get; set; } = [];
    }
}
