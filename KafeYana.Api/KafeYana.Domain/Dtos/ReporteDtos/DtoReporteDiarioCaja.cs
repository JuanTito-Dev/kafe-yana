namespace KafeYana.Application.Dtos.ReporteDtos
{
    /// <summary>
    /// Reporte diario de caja: lista TODAS las sesiones (históricas cerradas + la activa)
    /// cuya apertura cae en la fecha indicada (TZ La Paz). Permite al cajero/admin
    /// revisar lo que se hizo durante el día y, vinculado desde el KPICard "Cajas Abiertas"
    /// del Dashboard, ver el estado actual sin tener que ir al reporte de rango libre.
    /// </summary>
    public class DtoReporteDiarioCaja
    {
        public DateTime Fecha { get; set; }

        public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;

        public DtoResumenDiarioCaja Resumen { get; set; } = new();

        public List<DtoCajaDelDia> Cajas { get; set; } = new();

        /// <summary>Movimientos de todas las sesiones del día (cerradas + activa).</summary>
        public List<DtoMovimientoCajaDelDia> Movimientos { get; set; } = new();
    }

    public class DtoResumenDiarioCaja
    {
        /// <summary>Total de sesiones iniciadas en el día (incluye la activa si la hay).</summary>
        public int CajasIniciadas { get; set; }

        public int CajasCerradas { get; set; }

        /// <summary>True si existe una Caja con Abierta=true al momento de generar el reporte.</summary>
        public bool HayCajaAbierta { get; set; }

        public decimal TotalVentas { get; set; }

        public decimal TotalEfectivo { get; set; }

        public decimal TotalTarjeta { get; set; }

        public decimal TotalQr { get; set; }

        public decimal TotalIngresos { get; set; }

        /// <summary>Se devuelve como positivo (los egresos en BD son negativos).</summary>
        public decimal TotalEgresos { get; set; }

        /// <summary>Suma del SaldoInicial de cada sesión del día.</summary>
        public decimal SaldoInicialTotal { get; set; }

        /// <summary>TotalVentas + TotalIngresos - TotalEgresos.</summary>
        public decimal BalanceNeto { get; set; }
    }

    public class DtoCajaDelDia
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public DateTime Apertura { get; set; }

        public DateTime? Cierre { get; set; }

        public string AbiertaPor { get; set; } = string.Empty;

        public string? CerradaPor { get; set; }

        public decimal SaldoInicial { get; set; }

        public decimal TotalVentas { get; set; }

        public decimal TotalEfectivo { get; set; }

        public decimal TotalTarjeta { get; set; }

        public decimal TotalQr { get; set; }

        public decimal TotalIngresos { get; set; }

        /// <summary>Se devuelve como positivo.</summary>
        public decimal TotalEgresos { get; set; }

        public decimal Diferencia { get; set; }

        public string Estado { get; set; } = string.Empty;

        /// <summary>True para la sesión activa que aún no se cerró. La única sin Cierre.</summary>
        public bool AbiertaActualmente { get; set; }

        public List<DtoMovimientoCajaDelDia> Movimientos { get; set; } = new();
    }

    public class DtoMovimientoCajaDelDia
    {
        public int Id { get; set; }

        public string CodigoCaja { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Positivo para ingresos, negativo para egresos (igual que se persiste).</summary>
        public decimal Monto { get; set; }

        public string? Referencia { get; set; }

        public string? Nota { get; set; }
    }
}
