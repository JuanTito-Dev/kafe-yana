using KafeYana.Application.Dtos.Categoria;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ComboDtos
{
    public class DtoComboClient
    {
        [Required(ErrorMessage = "Nombre requerido")]
        public required string Nombre { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Valor no debe ser menor a 0")]
        public required decimal Precio { get; set; }

        public required List<ComboDetalleDto> Productos { get; set; }

        public Producto Crear()
        {
            var producto = new Producto
            {
                Nombre = this.Nombre,
                Descripcion = this.Descripcion,
                Precio = this.Precio,
                Tipo = TiposProductos.Promocion,
                Categoria_Id = 23,
                Promocion = new Promocion
                {
                    Detalles = this.Productos.Select(p => new PromocionDetalle
                    {
                        Id_Producto = p.ProductoId,
                        Cantidad = p.Cantidad,
                        Opcional = p.Opcional

                    }).ToList()
                }

            };

            return producto;
        }

        public void Actualizar(Producto producto)
        {
            producto.Nombre = this.Nombre;
            producto.Descripcion = this.Descripcion;
            producto.Precio = this.Precio;

            // Reemplazar los detalles
            producto.Promocion.Detalles = this.Productos.Select(p => new PromocionDetalle
            {
                Id_Producto = p.ProductoId,
                Cantidad = p.Cantidad,
                Opcional = p.Opcional,
                Id_Promocion = producto.Promocion.Id
            }).ToList();
        }

        public async Task ValidarProductos(IComboRepositorio db)
        {
            foreach (var p in Productos)
            {
                var producto = await db.FindByIdAsync(p.ProductoId);
                if (producto == null)
                    throw new ArgumentException($"El producto {p.ProductoId} no existe.");
                if (producto.Tipo == TiposProductos.Promocion)
                    throw new ArgumentException($"El producto {p.ProductoId} es un combo y no puede agregarse a otro combo.");
            }
        }
    }
}
