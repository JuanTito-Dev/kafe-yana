using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.CompradoDtos
{
    public class DtoCompradoGet : BaseEntity
    {
        public required string Nombre { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public required decimal Precio { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public required int Categoria_Id { get; set; }

        public string Codigo_barra { get; set; } = string.Empty;

        public required string Unidad_medida { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public required decimal Costo_compra { get; set; }

        public required int Stock_actual { get; set; }

        public required int Stock_minimo { get; set; }

        public required bool Disponible { get; set; }

        public static DtoCompradoGet Desde(Producto p) => new DtoCompradoGet
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Tipo = p.Tipo,
            Categoria_Id = p.Categoria_Id,
            Codigo_barra = p.Comprado.Codigo_barra,
            Unidad_medida = p.Comprado.Unidad_medida,
            Marca = p.Comprado.Marca,
            Ubicacion = p.Comprado.Ubicacion,
            Costo_compra = p.Comprado.Costo_compra,
            Stock_actual = p.Comprado.Stock_actual,
            Stock_minimo = p.Comprado.Stock_minimo,
            Disponible = p.Comprado.Disponible
        };
    }
}
