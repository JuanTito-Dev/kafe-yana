namespace KafeYana.Domain.Entities.Facturacion
{
    public class VerificaNitResult
    {
        public long Nit { get; set; }
        public bool Valido { get; set; }
        public bool Transaccion { get; set; }
        public List<MensajeSiat> Mensajes { get; set; } = new();
    }

    public class MensajeSiat
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
