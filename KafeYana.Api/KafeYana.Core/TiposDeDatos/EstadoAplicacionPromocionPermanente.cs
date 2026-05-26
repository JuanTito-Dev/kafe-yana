namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>Resultado de evaluar una promo en una venta (extensible a descuento / producto gratis).</summary>
    public static class EstadoAplicacionPromocionPermanente
    {
        public const string NoCalifica = "NoCalifica";
        public const string Califica = "Califica";
        public const string Aplicada = "Aplicada";
    }
}
