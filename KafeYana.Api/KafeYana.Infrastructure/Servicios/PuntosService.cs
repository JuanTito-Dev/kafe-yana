using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using System.Text;

namespace KafeYana.Infrastructure.Servicios
{
    public class PuntosService(IUnitWork _db) : IPuntosService
    {
        public async Task<int> CalcularYAplicarPuntosAsync(
            Cliente cliente,
            decimal totalVenta,
            bool tieneCombo,
            string codigoVenta)
        {
            var regla = await _db.reglaBasePuntos.ObtenerReglaAsync();

            if (regla is null || !regla.Activo || regla.Cantidad <= 0)
                return 0;

            var puntosBase = (int)Math.Floor(totalVenta / regla.Cantidad);

            if (puntosBase <= 0)
                return 0;

            var aceleradores = await _db.aceleradores.ObtenerTodosAsync();
            var ahora        = TimeOnly.FromDateTime(DateTime.Now);
            var esHoy        = cliente.Fecha_nacimiento.HasValue
                               && cliente.Fecha_nacimiento.Value.Month == DateTime.Today.Month
                               && cliente.Fecha_nacimiento.Value.Day   == DateTime.Today.Day;

            var desgloseBuilder  = new StringBuilder();
            var sumaMultiplicadores = 0;
            var sumaSumas           = 0;
            var hayMultiplicador    = false;

            foreach (var acelerador in aceleradores)
            {
                if (!acelerador.Activo) continue;

                switch (acelerador.Tipo)
                {
                    case TipoAcelerador.Combo when tieneCombo:
                        sumaSumas += (int)acelerador.Cantidad;
                        RegistrarDesglose(desgloseBuilder, $"Combo:+{acelerador.Cantidad}");
                        break;

                    case TipoAcelerador.CompraAlta when acelerador.UmbralMonto.HasValue
                                                     && totalVenta > acelerador.UmbralMonto:
                        sumaMultiplicadores += (int)(puntosBase * acelerador.Cantidad);
                        hayMultiplicador = true;
                        RegistrarDesglose(desgloseBuilder, $"CompraAlta:x{acelerador.Cantidad}={puntosBase * acelerador.Cantidad}");
                        break;

                    case TipoAcelerador.CompraMediana when acelerador.UmbralMonto.HasValue
                                                        && totalVenta > acelerador.UmbralMonto:
                        sumaSumas += (int)acelerador.Cantidad;
                        RegistrarDesglose(desgloseBuilder, $"CompraMediana:+{acelerador.Cantidad}");
                        break;

                    case TipoAcelerador.Cumpleanos when esHoy:
                        sumaMultiplicadores += (int)(puntosBase * acelerador.Cantidad);
                        hayMultiplicador = true;
                        RegistrarDesglose(desgloseBuilder, $"Cumpleanos:x{acelerador.Cantidad}={puntosBase * acelerador.Cantidad}");
                        break;

                    case TipoAcelerador.HoraValle when acelerador.HoraInicio.HasValue
                                                    && acelerador.HoraFin.HasValue
                                                    && ahora >= acelerador.HoraInicio
                                                    && ahora <= acelerador.HoraFin:
                        sumaSumas += (int)acelerador.Cantidad;
                        RegistrarDesglose(desgloseBuilder, $"HoraValle:+{acelerador.Cantidad}");
                        break;
                }
            }

            // Si hubo multiplicadores, el total de multiplicados reemplaza a la base.
            // Si no hubo ninguno, se usa la base directamente.
            var puntosDesdeMultiplicadores = hayMultiplicador ? sumaMultiplicadores : puntosBase;
            var puntosFinales              = puntosDesdeMultiplicadores + sumaSumas;

            cliente.AgregarPuntos(puntosFinales);

            await _db.historialPuntos.Crear(new HistorialPuntos
            {
                Id_Cliente    = cliente.Id,
                CodigoVenta   = codigoVenta,
                PuntosBase    = puntosBase,
                PuntosFinales = puntosFinales,
                Desglose      = desgloseBuilder.Length > 0 ? desgloseBuilder.ToString().TrimEnd('|', ' ') : null,
                Fecha         = DateTime.UtcNow
            });

            return puntosFinales;
        }

        private static void RegistrarDesglose(StringBuilder sb, string texto)
        {
            if (sb.Length > 0) sb.Append(" | ");
            sb.Append(texto);
        }
    }
}
