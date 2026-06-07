using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities.Facturacion
{
    public class CodigoSiat : BaseEntity
    {
        public string CodigoProducto { get; set; } = string.Empty;
        public string DescripcionProducto { get; set; } = string.Empty;
        public string CodigoActividad { get; set; } = string.Empty;
        public string DescripcionActividad { get; set; } = string.Empty;
    }
}
