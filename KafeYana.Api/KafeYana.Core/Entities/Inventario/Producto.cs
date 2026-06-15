using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Producto : BaseEntity
    {
        public required string Nombre { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public required decimal Precio { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string UrlImagen { get; set; } = string.Empty;

        /// <summary>Código interno del sistema según Id. Ej: 00001. Distinto de CodigoSin.</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Código de producto SIN para facturación electrónica.</summary>
        public string CodigoSin { get; set; } = string.Empty;

        //Fk for categoria 

        public required int Categoria_Id { get; set; }

        //Navegacion

        public Categoria Categoria { get; set; } 

        public Comprado? Comprado { get; set; }

        public Elaborado Elaborado { get; set; }

        public Promocion Promocion  { get; set; }

        public ICollection<PromocionDetalle> Detalles { get; set; } = new List<PromocionDetalle>();

        public List<Detalle_ronda> Detalle_Rondas { get; set; } = new List<Detalle_ronda>();

        public List<ProductoMovimiento> Movimientos { get; set; } = new List<ProductoMovimiento>();

        public List<OrdenItemProducto> OrdenesProducto { get; set; } = new List<OrdenItemProducto>();

        public void AsignarCodigo(string codigo) =>
            Codigo = codigo;

        internal ProductoMovimiento Movimiento(int Cantidad, string Tipo, string referencia, int StockResultado, decimal costo)
        { 
            var NuevoMovimiento = new ProductoMovimiento(Id, Tipo, referencia, Cantidad, costo, StockResultado);

            return NuevoMovimiento;
        }


    }
}
