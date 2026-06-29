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

    /// <summary>
    /// DTO SOAP para <c>registroEventoSignificativo</c>. Mapea el sobre que
    /// arma <c>SiatHttpClient.RegistroEventoSignificativoAsync</c>.
    /// Estructura del request según WSDL SIAT (confirmado por ejemplo en
    /// producción jun-2026):
    /// <code>
    /// &lt;SolicitudEventoSignificativo&gt;
    ///   &lt;codigoAmbiente/&gt; &lt;codigoPuntoVenta/&gt; &lt;codigoSistema/&gt;
    ///   &lt;codigoSucursal/&gt; &lt;cufd/&gt; &lt;cufdEvento/&gt; &lt;cuis/&gt;
    ///   &lt;descripcion/&gt; &lt;fechaHoraInicioEvento/&gt; &lt;fechaHoraFinEvento/&gt;
    ///   &lt;nit/&gt; &lt;codigoMotivoEvento/&gt;
    /// &lt;/SolicitudEventoSignificativo&gt;
    /// </code>
    /// Respuesta: <c>&lt;RespuestaListaEventos&gt;</c> con <c>codigoRecepcionEventoSignificativo</c>
    /// + <c>transaccion</c>. Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class SolicitudRegistroEventoSignificativoSiatDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string CufdEvento { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHoraInicioEvento { get; set; }
        public DateTime FechaHoraFinEvento { get; set; }
        public long Nit { get; set; }
        public int CodigoMotivoEvento { get; set; }
    }

    /// <summary>Respuesta cruda SOAP de <c>registroEventoSignificativoResponse</c>.</summary>
    public class RespuestaRegistroEventoSignificativoSiatDto
    {
        public bool Transaccion { get; set; }
        public string? CodigoRecepcionEventoSignificativo { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuesta> CodigosRespuesta { get; set; } = new();
    }
}
