namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public sealed class ResultadoImpresionFacturaDto
    {
        public bool Enviado { get; init; }

        public bool Ok { get; init; }

        public string? ErrorMensaje { get; init; }

        public string? UrlQr { get; init; }
    }
}
