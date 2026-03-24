using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Comprado : BaseEntity 
    {
        public string Codigo_barra { get; set; } = string.Empty;

        public required string Unidad_medida { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public required decimal Costo_compra { get; set; } 

        public required int Stock_actual { get; set; }

        public required int Stock_minimo { get; set; }

        public required bool Disponible { get; set; }

        //FK
        public int Id_Producto { get; set; }
        
        //Navegacion
        public Producto Producto { get; set; }

    }
}
