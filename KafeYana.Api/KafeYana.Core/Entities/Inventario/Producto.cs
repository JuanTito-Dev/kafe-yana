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

        //Fk for categoria 

        public required int Categoria_Id { get; set; }

        //Navegacion

        public Categoria Categoria { get; set; } 

        public Comprado Comprado { get; set; }

        public Elaborado Elaborado { get; set; }

        public Promocion Promocion  { get; set; }

        public ICollection<PromocionDetalle> Detalles { get; set; } = new List<PromocionDetalle>();

        public List<Detalle_ronda> Detalle_Rondas { get; set; } = new List<Detalle_ronda>();

    }
}
