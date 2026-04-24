using HotChocolate.Authorization;
using KafeYana.Application.Dtos.ComboDtos;
using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ComboQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]

        public IQueryable<Promocion> Combos([Service] IComboRepositorio _db)
        {

            return _db.GetCombos();

        }  

        //[Authorize(Roles = new[] { "Admin" })]
        //[UseProjection]
        //public async Task<DtoComboGet?> Combo([Service] AppDbContext _db, int Id)
        //{
        //    if (Id <= 0) return null;

        //    var raw = await _db.Productos
        //        .Where(x => x.Id == Id && x.Tipo == TiposProductos.Promocion)
        //        .Select(p => new
        //        {
        //            p.Id,
        //            p.Nombre,
        //            p.Descripcion,
        //            p.Precio,
        //            p.Tipo,
        //            p.Categoria_Id,
        //            Detalles = p.Promocion.Detalles.Select(d => new
        //            {
        //                d.Id_Producto,
        //                d.Cantidad,
        //                d.Opcional,
        //                TipoProducto = d.Producto.Tipo,
        //                StockComprado = d.Producto.Comprado != null
        //                    ? (decimal?)d.Producto.Comprado.Stock_actual
        //                    : null,
        //                StockElaborado = d.Producto.Elaborado != null && d.Producto.Elaborado.Receta != null
        //                    ? d.Producto.Elaborado.Receta.Detalles.Select(det => new
        //                    {
        //                        det.Cantidad,
        //                        det.Merma,
        //                        StockInsumo = (decimal)det.Insumo.Stock_actual
        //                    }).ToList()
        //                    : null
        //            }).ToList()
        //        })
        //        .FirstOrDefaultAsync();

        //    if (raw == null) return null;

        //    // Cálculo en memoria
        //    return new DtoComboGet
        //    {
        //        Id = raw.Id,
        //        Nombre = raw.Nombre,
        //        Descripcion = raw.Descripcion,
        //        Precio = raw.Precio,
        //        Tipo = raw.Tipo,
        //        Categoria_Id = raw.Categoria_Id,
        //        Productos = raw.Detalles.Select(d => new ComboDetalleDto
        //        {
        //            ProductoId = d.Id_Producto,
        //            Cantidad = d.Cantidad,
        //            Opcional = d.Opcional
        //        }).ToList(),
        //        CantidadProducible = raw.Detalles
        //            .Where(d => !d.Opcional)
        //            .Select(d => d.TipoProducto == TiposProductos.Comprado
        //                ? (d.StockComprado.HasValue
        //                    ? (int)(d.StockComprado.Value / d.Cantidad)
        //                    : 0)
        //                : (d.StockElaborado != null && d.StockElaborado.Any()
        //                    ? d.StockElaborado
        //                        .Select(det => (int)(det.StockInsumo /
        //                            (det.Cantidad * (1 + det.Merma / 100m))))
        //                        .Min()
        //                    : 0)
        //            )
        //            .DefaultIfEmpty(0)
        //            .Min()
        //    };
        //}
    }
}

