namespace KafeYana.Infrastructure.Options
{
    public class ImpresoraOptions
    {
        public const string Key = "Impresoras";

        public bool DevMode { get; set; } = true;
        public Dictionary<string, DestinoConfig> Destinos { get; set; } = [];
    }

    public class DestinoConfig
    {
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
