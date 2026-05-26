using KafeYana.Application.Dtos.ProductoCanjeable;

namespace KafeYana.Application.IServicios
{
    public interface ICanjeProductoService
    {
        /// <summary>Valida disponibilidad, stock e impacta inventario + puntos.</summary>
        Task EjecutarCanjeAsync(DtoCanjeProducto dto);
    }
}
