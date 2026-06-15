namespace KafeYana.Infrastructure.Configuration
{
    public class FacturaImpresoraOptions
    {
        public const string SeccionNombre = "FacturaImpresora";

        public bool DevMode { get; set; } = true;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; } = 9100;

        public int AnchoCaracteres { get; set; } = 48;

        public bool AutoImprimirAlCobrar { get; set; } = true;
    }
}
