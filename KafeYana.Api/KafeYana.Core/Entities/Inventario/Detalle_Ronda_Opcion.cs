namespace KafeYana.Domain.Entities.Inventario
{
    public class Detalle_Ronda_Opcion
    {
        public int Id_Detalle_Ronda { get; set; }

        public int Id_Opcion { get; set; }

        public string TipoOpcion { get; set; } = "normal";

        public string? ValorAnterior { get; set; }

        public decimal? CostoExtra { get; set; }

        public Detalle_ronda? Detalle_Ronda { get; set; }

        public Opcion? Opcion { get; set; }
    }
}
