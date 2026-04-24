using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.RecetaDtos
{
    public class DtoRecetaGet : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;

        public int Porciones { get; set; }
        public int Id_Elaborado { get; set; }

        public IReadOnlyList<DtoDetalleGet> Detalles { get; set; } = [];

        public static DtoRecetaGet Desde(Receta receta) => new DtoRecetaGet
        {
            Id = receta.Id,
            Nombre = receta.Nombre,
            Nota = receta.Nota,
            Porciones = receta.Porciones,
            Id_Elaborado = receta.Elaborado is null? 0 : receta.Elaborado.Id_Producto,
            Detalles = receta.Detalles.Select(x => new DtoDetalleGet
            {
                Cantidad = x.Cantidad,
                Merma = x.Merma,
                SubTotal = x.SubTotal,
                Id_insumo = x.Id_insumo,
                Nombre = x.Insumo is null? string.Empty : x.Insumo.Nombre
            }).ToList()
        };
    }
}
