using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.TiposDeDatos
{
    public class TiposProductos
    {
        public static string Comprado { get; } = "Comprado" ;

        public static string Elaborado { get; } = "Elaborado";

        public static string Promocion { get; } = "Combos";
    }
}
