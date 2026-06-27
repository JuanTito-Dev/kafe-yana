using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Catalogos;

namespace KafeYana.Domain.Entities
{
    public class Cliente : BaseEntity
    {
        public int? Dni { get; set; }

        public required string Nombre { get; set; }

        /// <summary>Código único de facturación SIAT. Ej: J-0002.</summary>
        public string Codigo { get; set; } = string.Empty;

        public string? Celular { get; set; }

        public string? Correo { get; set; }

        public string? Correonormalizado { get; set; }

        public DateTime? Fecha_nacimiento { get; set; }

        public string? Direccion { get; set; }

        public int Puntos { get; private set; } = 0;

        public int NumeroCompras { get; private set; } = 0;

        /// <summary>
        /// FK opcional a <see cref="Catalogos.CatPaisOrigen"/>. Sólo se popula
        /// para clientes extranjeros (CEX / PAS). Bolivianos (CI / NIT / OD)
        /// son implícitamente país=22 (BOLIVIA) y este campo queda null.
        /// El frontend envía el código SIN (1..211) en <c>DtoVentaPedido.CodigoPaisOrigen</c>
        /// y <c>ClientePedidoHelper</c> lo resuelve a este FK local.
        /// </summary>
        public int? IdPaisOrigen { get; set; }

        /// <summary>Navigation property al país (no se persiste directo, es via FK).</summary>
        public CatPaisOrigen? PaisOrigen { get; set; }

        public void AgregarPuntos(int cantidad)
        {
            if (cantidad <= 0) return;
            Puntos += cantidad;
        }

        /// <summary>Descuenta puntos ya validados en la capa de servicio (saldo suficiente).</summary>
        public void DescontarPuntosPorCanje(int cantidad)
        {
            if (cantidad <= 0)
                return;

            Puntos -= cantidad;
        }

        public void RegistrarCompra()
        {
            NumeroCompras++;
        }

        public bool Estado { get; set; } = true;

        public List<Pedido> Pedidos { get; set; } = new();

        public void AsignarCodigoFacturacion(string codigo) =>
            Codigo = codigo;
    }
}
