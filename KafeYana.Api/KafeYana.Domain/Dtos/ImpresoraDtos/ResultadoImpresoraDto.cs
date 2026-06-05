namespace KafeYana.Application.Dtos.ImpresoraDtos
{
    public class ResultadoImpresoraDto
    {
        public string Destino { get; set; } = string.Empty;
        public bool Ok { get; set; }
        public string? Error { get; set; }
    }
}
