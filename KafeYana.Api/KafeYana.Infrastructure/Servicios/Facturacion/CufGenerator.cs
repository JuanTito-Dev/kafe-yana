using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using System.Globalization;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class CufGenerator : ICufGenerator
    {
        public string Generar(CufGeneracionRequest request)
        {
            var cadena53 = string.Concat(
                SiatStringTools.CompletarCeros(request.Nit.ToString(), 13),
                SiatFechaEmision.FormatearParaCuf(request.FechaEmision),
                SiatStringTools.CompletarCeros(request.CodigoSucursal.ToString(), 4),
                request.CodigoModalidad.ToString(),
                request.TipoEmision.ToString(),
                request.TipoFacturaDocumento.ToString(),
                SiatStringTools.CompletarCeros(request.CodigoDocumentoSector.ToString(), 2),
                SiatStringTools.CompletarCeros(request.NumeroFactura.ToString(), 10),
                SiatStringTools.CompletarCeros(request.CodigoPuntoVenta.ToString(), 4));

            var digitoVerificador = CalcularDigitoMod11(cadena53);
            var cadena54 = cadena53 + digitoVerificador;
            var base16 = SiatStringTools.Base16(cadena54);

            return base16 + request.CodigoControl;
        }

        /// <summary>Algoritmo módulo 11 SIAT (numDig=1, limMult=9, x10=false).</summary>
        private static string CalcularDigitoMod11(string cadena)
        {
            var suma = 0;
            var mult = 2;

            for (var i = cadena.Length - 1; i >= 0; i--)
            {
                suma += mult * (cadena[i] - '0');
                if (++mult > 9)
                    mult = 2;
            }

            var dig = suma % 11;

            if (dig == 10)
                return "1";
            if (dig == 11)
                return "0";

            return dig.ToString(CultureInfo.InvariantCulture);
        }
    }
}
