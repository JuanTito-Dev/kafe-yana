namespace KafeYana.Infrastructure.Options
{
    public class ImpresoraOptions
    {
        public const string Key = "Impresoras";

        public bool DevMode { get; set; } = true;

        /// <summary>
        /// Ancho por defecto en caracteres para tickets (comanda/cuenta/factura).
        /// 48 = 80mm@FontA (default Bolivia). Se puede sobreescribir por request.
        /// </summary>
        public int AnchoCaracteres { get; set; } = 48;

        public Dictionary<string, DestinoConfig> Destinos { get; set; } = [];
    }

    public class DestinoConfig
    {
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
