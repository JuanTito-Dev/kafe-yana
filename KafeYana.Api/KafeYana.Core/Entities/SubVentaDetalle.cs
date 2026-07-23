using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Copia autocontenida de una porción de producto cobrada por una <see cref="SubVenta"/>.
    /// Nunca depende de la fila de Detalle_ronda que la originó: si esa fila se edita o
    /// borra después, esta línea no se entera y no se rompe.
    /// </summary>
    public class SubVentaDetalle : BaseEntity
    {
        public int Id_SubVenta { get; set; }
        public SubVenta? SubVenta { get; set; }

        /// <summary>Id del catálogo (Producto.Id) — nunca el id de la fila de Detalle_ronda.</summary>
        public int Id_Producto { get; set; }
        public string Nombre_Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }

        /// <summary>Precio vigente en la ronda de origen al momento del cobro.</summary>
        public decimal Precio { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public string CodigoSin { get; set; } = string.Empty;
        public int CodigoUnidadMedida { get; set; } = 57;

        /// <summary>
        /// Solo auditoría/trazabilidad de qué ronda se descontó. NO es una relación EF,
        /// no tiene FK en base de datos, nunca se usa para lógica de negocio.
        /// </summary>
        public int? OrigenRondaId { get; set; }
    }
}
