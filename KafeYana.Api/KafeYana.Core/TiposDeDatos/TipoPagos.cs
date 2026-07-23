namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>
    /// Códigos oficiales SIAT para los métodos de pago "simples"
    /// (los más usados en el día a día de una cafetería).
    ///
    /// Bugfix jun-2026: el código <c>Qr = 32</c> que tenía la versión anterior
    /// NO existe en el catálogo SIN oficial. El código correcto para
    /// transferencia/QR es <c>7 = TRANSFERENCIA BANCARIA</c>.
    /// </summary>
    public enum TipoPagos
    {
        Efectivo = 1,
        Tarjeta = 2,
        Otros = 5,
        Transferencia = 7
    }
}