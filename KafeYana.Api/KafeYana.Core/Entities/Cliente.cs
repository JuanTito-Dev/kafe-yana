using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    public class Cliente : BaseEntity
    {
        public int? Dni { get; set; }

        public required string Nombre { get; set; }

        /// <summary>Código único de facturación SIAT. Ej: J-0002.</summary>
        public string Codigo { get; set; } = string.Empty;

        public required string Celular { get; set; }  

        public string? Correo { get; set; }  

        public string? Correonormalizado { get; set; } 

        public DateTime? Fecha_nacimiento { get; set; }

        public string? Direccion { get; set; }

        public int Puntos { get; private set; } = 0;

        public int NumeroCompras { get; private set; } = 0;

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
