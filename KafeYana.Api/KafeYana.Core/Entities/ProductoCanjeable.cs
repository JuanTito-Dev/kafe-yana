using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Domain.Entities
{
    public class ProductoCanjeable : BaseEntity
    {
        public int Id_Producto { get; set; }

        public Producto? Producto { get; set; }

        /// <summary>Nombre del producto copiado al crear para evitar joins.</summary>
        public required string NombreProducto { get; set; }

        /// <summary>Categoría copiada al crear para evitar joins.</summary>
        public required string Categoria { get; set; }

        public int Puntos { get; set; }

        /// <summary>Mesas | ParaLlevar | MesasYParaLlevar</summary>
        public required string Disponible { get; set; }

        public bool Activo { get; set; } = true;
    }
}
