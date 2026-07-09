using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class ClientePedidoHelper
    {
        // Debe coincidir con CONSUMIDOR_FINAL_NAME en frontend/src/constants/facturacion.ts.
        private const string ConsumidorFinalNombre = "Consumidor Final";

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

            return await ResolverOCrearClientePorDocumentoAsync(db, nombre.Trim(), dni.Value, codigoPaisOrigen: null);
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
                var cliente = await ResolverOCrearClientePorDocumentoAsync(db, datos.Nombre.Trim(), dni, datos.CodigoPaisOrigen);
                return (cliente, dni.ToString());
            }

            if (pedido.Id_Cliente is int pedidoClienteId && pedidoClienteId > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, pedidoClienteId);
                return (cliente, cliente.Dni?.ToString() ?? "0");
            }

            // Cliente no es obligatorio para cobrar: sin selección, se cobra a
            // "Consumidor Final" (se crea en BD la primera vez que se necesita).
            var cf = await ResolverConsumidorFinalAsync(db);
            return (cf, cf.Dni?.ToString() ?? "0");
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
                var cliente = await ResolverOCrearClientePorDocumentoAsync(db, datos.Nombre.Trim(), dni, datos.CodigoPaisOrigen);
                return (cliente, dni.ToString());
            }

            if (pedido.Id_Cliente is int pedidoClienteId && pedidoClienteId > 0)
            {
                var cliente = await ObtenerClienteDelPedidoAsync(db, pedido, pedidoClienteId);
                return (cliente, ObtenerNumeroDocumento(cliente));
            }

            // Cliente no es obligatorio para facturar: sin selección, se factura a
            // "Consumidor Final" (NIT, numeroDocumento="0").
            var cf = await ResolverConsumidorFinalAsync(db);
            return (cf, cf.Dni?.ToString() ?? "0");
        }

        private static async Task<Cliente> ResolverConsumidorFinalAsync(IUnitWork db)
        {
            var existente = await db.clientes.GetByNombreAsync(ConsumidorFinalNombre);
            if (existente is not null)
                return existente;

            var cf = new Cliente
            {
                Nombre = ConsumidorFinalNombre,
                Dni = null,
                Celular = null,
                Correo = null,
                Correonormalizado = string.Empty,
                Estado = true
            };

            await db.clientes.Crear(cf);
            await db.SaveUnitWork();
            cf.AsignarCodigoFacturacion(ClienteCodigoService.Generar(cf.Nombre, cf.Id));

            return cf;
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

        private static async Task<Cliente> ResolverOCrearClientePorDocumentoAsync(
            IUnitWork db,
            string nombre,
            int dni,
            int? codigoPaisOrigen)
        {
            // Resolver código SIN → Id local una sola vez (reusado abajo para
            // cliente existente y cliente nuevo).
            int? idPaisOrigen = await ResolverIdPaisOrigenAsync(db, codigoPaisOrigen);

            var existente = await db.clientes.GetByDniAsync(dni);
            if (existente is not null)
            {
                if (!string.Equals(existente.Nombre, nombre, StringComparison.Ordinal))
                    existente.Nombre = nombre;

                // País: completar si estaba null O si el cajero está corrigiendo
                // el país de un cliente viejo. NO tocar si el body no trae país.
                if (idPaisOrigen is int idPais && existente.IdPaisOrigen != idPais)
                    existente.IdPaisOrigen = idPais;

                return existente;
            }

            var nuevoCliente = new Cliente
            {
                Nombre = nombre,
                Dni = dni,
                Celular = null,
                Correo = null,
                Correonormalizado = string.Empty,
                IdPaisOrigen = idPaisOrigen,
                Estado = true
            };

            await db.clientes.Crear(nuevoCliente);
            await db.SaveUnitWork();
            nuevoCliente.AsignarCodigoFacturacion(
                ClienteCodigoService.Generar(nuevoCliente.Nombre, nuevoCliente.Id));

            return nuevoCliente;
        }

        /// <summary>
        /// Resuelve el código SIN (1..211) del país de origen al FK local
        /// <c>CatPaisOrigen.Id</c>. Null si el body no trae código o si el
        /// código no existe en el catálogo (caso raro: catálogo no sincronizado).
        /// </summary>
        private static async Task<int?> ResolverIdPaisOrigenAsync(IUnitWork db, int? codigoSiat)
        {
            if (codigoSiat is not int codigo || codigo <= 0)
                return null;

            var pais = await db.catPaisesOrigen.GetByCodigoAsync(codigo);
            if (pais is null)
            {
                throw new VentaException(
                    $"El código de país de origen {codigo} no existe en el catálogo SIAT. " +
                    "Sincronice el catálogo (POST /api/catalogos/sincronizar-paises-origen) " +
                    "o pida al cliente un código válido.");
            }

            return pais.Id;
        }
    }
}
