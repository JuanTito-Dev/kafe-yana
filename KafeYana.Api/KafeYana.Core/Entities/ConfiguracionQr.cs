using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    public class ConfiguracionQr : BaseEntity
    {
        public string Url { get; set; } = string.Empty;
    }
}
