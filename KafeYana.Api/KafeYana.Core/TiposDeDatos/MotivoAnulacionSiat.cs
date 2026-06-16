namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>Paramétrica SIAT codigoMotivo para anulación de factura.</summary>
    public enum MotivoAnulacionSiat
    {
        FacturaMalEmitida = 1,
        NotaCreditoDebito = 2,
        DatosClienteIncorrectos = 3,
        Otros = 4
    }

    public static class MotivoAnulacionSiatCatalogo
    {
        public static bool EsValido(int codigo) =>
            Enum.IsDefined(typeof(MotivoAnulacionSiat), codigo);

        public static string ObtenerDescripcion(int codigo) => codigo switch
        {
            1 => "Factura mal emitida",
            2 => "Nota de crédito/débito",
            3 => "Datos del cliente incorrectos",
            4 => "Otros",
            _ => $"Motivo {codigo}"
        };
    }
}
