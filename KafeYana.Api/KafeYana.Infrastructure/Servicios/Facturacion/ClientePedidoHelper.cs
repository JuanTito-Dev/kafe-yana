using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class ClientePedidoHelper
    {
        public static async Task<Cliente?> VincularClienteAlPedidoAsync(
            IUnitWork db,
            int? idCliente,
            string? nombre,
            int? dni)
        {
            if (idCliente is int id && id > 0)
            {
                var existente = await db.clientes.FindByIdAsync(id);
                if (existente is null)
                    throw new InventarioException("Cliente no encontrado.");

                return existente;
            }

            if (string.IsNullOrWhiteSpace(nombre) && dni is null)
                return null;

            if (string.IsNullOrWhiteSpace(nombre) || dni is null or <= 0)
            {
                throw new VentaException(
                    "Si no envía Id_Cliente, el Nombre y la C.L. son obligatorios.");
            }

            return await CrearClienteAsync(db, nombre.Trim(), dni.Value);
        }

        public static async Task<(Cliente Cliente, string NumeroDocumento)> ResolverClienteParaCobroAsync(
            IUnitWork db,
            DtoVentaPedido datos,
            Pedido pedido)
        {
            if (datos.Factura)
                return await ResolverParaFacturacionAsync(db, datos, pedido);

            if (datos.Id_Cliente is int idCliente && idCliente > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, idCliente);
                return (cliente, cliente.Dni?.ToString() ?? "0");
            }

            if (!string.IsNullOrWhiteSpace(datos.Nombre) && datos.Dni is int dni && dni > 0)
            {
                var cliente = await CrearClienteAsync(db, datos.Nombre.Trim(), dni);
                return (cliente, dni.ToString());
            }

            if (pedido.Id_Cliente is int pedidoClienteId && pedidoClienteId > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, pedidoClienteId);
                return (cliente, cliente.Dni?.ToString() ?? "0");
            }

            throw new VentaException("Debe indicar el cliente para registrar el cobro.");
        }

        public static async Task<(Cliente Cliente, string NumeroDocumento)> ResolverParaFacturacionAsync(
            IUnitWork db,
            DtoVentaPedido datos,
            Pedido pedido)
        {
            if (datos.Id_Cliente is int idCliente && idCliente > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, idCliente);
                return (cliente, ObtenerNumeroDocumento(cliente));
            }

            if (!string.IsNullOrWhiteSpace(datos.Nombre) && datos.Dni is int dni && dni > 0)
            {
                var cliente = await CrearClienteAsync(db, datos.Nombre.Trim(), dni);
                return (cliente, dni.ToString());
            }

            if (pedido.Id_Cliente is int pedidoClienteId && pedidoClienteId > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, pedidoClienteId);
                return (cliente, ObtenerNumeroDocumento(cliente));
            }

            throw new VentaException(
                "Debe enviar Id_Cliente o Nombre y C.L., o el pedido debe tener un cliente asignado.");
        }

        private static async Task<Cliente> ObtenerClienteDelPedidoAsync(
            IUnitWork db,
            Pedido pedido,
            int idCliente)
        {
            Cliente? cliente = pedido.Cliente;
            if (pedido.Id_Cliente is null || pedido.Id_Cliente != idCliente)
            {
                cliente = await db.clientes.FindByIdAsync(idCliente);
            }
            else if (cliente is null)
            {
                cliente = await db.clientes.FindByIdAsync(idCliente);
            }

            if (cliente is null)
                throw new InventarioException("Cliente no encontrado.");

            return cliente;
        }

        private static string ObtenerNumeroDocumento(Cliente cliente)
        {
            if (!cliente.Dni.HasValue)
                throw new VentaException("El cliente no tiene C.L. registrada.");

            return cliente.Dni.Value.ToString();
        }

        private static async Task<Cliente> CrearClienteAsync(IUnitWork db, string nombre, int dni)
        {
            var existente = await db.clientes.GetByDniAsync(dni);
            if (existente is not null)
            {
                throw new VentaException(
                    $"Ya existe un cliente registrado con el número de documento {dni}. " +
                    "Envíe Id_Cliente para usar ese cliente en el cobro.");
            }

            var nuevoCliente = new Cliente
            {
                Nombre = nombre,
                Dni = dni,
                Celular = null,
                Correo = null,
                Correonormalizado = string.Empty,
                Estado = true
            };

            await db.clientes.Crear(nuevoCliente);
            await db.SaveUnitWork();
            nuevoCliente.AsignarCodigoFacturacion(
                ClienteCodigoService.Generar(nuevoCliente.Nombre, nuevoCliente.Id));

            return nuevoCliente;
        }
    }
}
