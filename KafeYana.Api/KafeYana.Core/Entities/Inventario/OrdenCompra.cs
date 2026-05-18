using HotChocolate;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class OrdenCompra : BaseEntity
    {
        public string Codigo { get; set; } 
        public required string Nombre_Proveedor { get; set; }
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public bool Recibido { get; private set; } = false;

        public string Estado { get; private set; } = EstadoOrdenes.Pendiente;
        public decimal Total { get; set; } = 0.00M;
        public string Nota { get; set; } = string.Empty;
        public int? Id_Proveedor { get; set; }
        public Proveedor? Proveedor { get; set; }
        public List<OrdenItemInsumo> Insumos { get; set; } = new List<OrdenItemInsumo>();
        public List<OrdenItemProducto> Productos { get; set; } = new List<OrdenItemProducto>();

        [GraphQLIgnore]
        public void Recibir()
        {
            Recibido = true;
            Estado = EstadoOrdenes.Recibido;
        }

        public void Cancelar()
        {
            Estado = EstadoOrdenes.Cancelado;
            Recibido = false;
        }

        public void CortarRelaciones()
        {
            foreach (var item in Insumos)
                item.Id_Insumo = null;

            foreach (var item in Productos)
                item.Id_Producto = null;
        }
    }
}
