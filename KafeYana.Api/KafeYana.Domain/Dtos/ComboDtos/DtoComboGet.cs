using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ComboDtos
{
    public class DtoComboGet : BaseEntity
    {
        public required string Nombre { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public required decimal Precio { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int CantidadProducible { get; set; }
        public required int Categoria_Id { get; set; }
        public List<ComboDetalleDto> Productos { get; set; } = new();

        public static DtoComboGet Desde(Producto p) => new DtoComboGet
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Tipo = p.Tipo,
            Categoria_Id = p.Categoria_Id,
            Productos = p.Promocion.Detalles.Select(d => new ComboDetalleDto
            {
                ProductoId = d.Id_Producto,
                Cantidad = d.Cantidad,
                Opcional = d.Opcional
            }).ToList()
        };


    }


}
