using KafeYana.Application.IRepositorio;

namespace KafeYana.Application.IServicios;

/// <summary>Descuenta inventario al canjear o reclamar producto (misma regla que pedidos).</summary>
public interface IInventarioCanjeService
{
    Task DescontarInventarioAsync(int idProducto, int cantidad, string referencia);
}
