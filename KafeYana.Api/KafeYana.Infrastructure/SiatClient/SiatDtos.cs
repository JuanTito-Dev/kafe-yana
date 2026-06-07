using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.SiatClient
{
    /// <summary>Respuesta del servicio CUIS</summary>
    public class RespuestaCuis
    {
        public string? CodigoCuis { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public bool Transaccion { get; set; }
        public List<CodigoRespuesta> CodigosRespuesta { get; set; } = new();
    }

    /// <summary>Respuesta del servicio CUFD</summary>
    public class RespuestaCufd
    {
        public string? CodigoCufd { get; set; }
        public string? CodigoControl { get; set; }
        public string? Direccion { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public bool Transaccion { get; set; }
        public List<CodigoRespuesta> CodigosRespuesta { get; set; } = new();
    }

    /// <summary>Respuesta del servicio VerificaNIT</summary>
    public class RespuestaVerificaNit
    {
        public bool Transaccion { get; set; }
        public List<CodigoRespuesta> Mensajes { get; set; } = new();
    }

    /// <summary>Código de respuesta/error que retorna el SIAT</summary>
    public class CodigoRespuesta
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
