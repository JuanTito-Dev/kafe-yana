using KafeYana.Domain.Entities.BaseEntidades;
using System.Collections.Generic;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Detalle_ronda : BaseEntity
    {
        public int Id_Ronda { get; set; }

        public required string Nombre_Producto { get; set; }

        public required int Cantidad { get; set; }

        public decimal Precio { get; set; } = 0.00M;

        /// <summary>Clave foránea hacia <see cref="Producto"/> (tabla Producto).</summary>
        public int Id_Producto { get; set; }

        public Producto? Producto { get; set; }

        public string Nota { get; set; } = string.Empty;

        /// <summary>Ubicación del producto. String.Empty cuando la línea es un combo (los items tienen su propia ubicación).</summary>
        public string Ubicacion { get; set; } = string.Empty;

        /// <summary>Código interno del producto (snapshot al crear la ronda). Ej: 00001.</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Código SIN del producto (snapshot al crear la ronda).</summary>
        public string CodigoSin { get; set; } = string.Empty;

        /// <summary>Código SIAT de unidad de medida (snapshot al crear la ronda).</summary>
        public int CodigoUnidadMedida { get; set; } = 57;

        public Ronda? ronda { get; set; }

        public List<Detalle_Ronda_Opcion> Opciones { get; set; } = new List<Detalle_Ronda_Opcion>();

        /// <summary>Solo se rellena cuando la línea es un combo (promoción): un registro por producto del combo.</summary>
        public List<Detalle_Ronda_ComboItem> ItemsCombo { get; set; } = new List<Detalle_Ronda_ComboItem>();

        public PedidoInventarioComprometido? CompromisoInventario { get; set; }

        /// <summary>Cantidad de unidades ya descontadas por sub-ventas (cobros parciales). Piso mínimo editable.</summary>
        public int CantidadDescontada { get; set; } = 0;
    }
}
