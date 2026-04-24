using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ElaboradosDtos
{
    public class DtoElaboradoGet : BaseEntity
    {
        public required string Nombre { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public required decimal Precio { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public required int Categoria_Id { get; set; }
        public required string Unidad_medida { get; set; }

        public int CantidadProducible { get; set; }

        public List<DtoVariacionGet> Variaciones { get; set; } = new List<DtoVariacionGet>();

        public static DtoElaboradoGet Desde(Producto p) => new DtoElaboradoGet
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Tipo = p.Tipo,
            Categoria_Id = p.Categoria_Id,
            Unidad_medida = p.Elaborado.Unidad_medida
        };
    }
}
