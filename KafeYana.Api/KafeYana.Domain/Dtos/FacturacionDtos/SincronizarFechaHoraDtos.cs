using System;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Resultado de la operación SOAP "sincronizarFechaHora".
    /// Devuelve la fecha/hora oficial del SIN para usar en la factura/nota.
    /// </summary>
    public class SincronizarFechaHoraResponse
    {
        public bool Transaccion { get; set; }
        public DateTime? FechaHora { get; set; }
    }
}