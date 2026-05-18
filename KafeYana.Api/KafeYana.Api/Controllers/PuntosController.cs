using KafeYana.Application.Dtos.PuntosDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuntosController(IUnitWork _db) : ControllerBase
    {
        // ─── Configuración: Regla base ────────────────────────────────────────

        [HttpGet("config/reglabase")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ObtenerReglaBase()
        {
            var regla = await _db.reglaBasePuntos.ObtenerReglaAsync();
            if (regla is null)
                return NotFound(new { message = "Regla base no configurada" });

            return Ok(new { regla.Id, regla.Cantidad, regla.Activo });
        }

        [HttpPost("config/reglabase")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> CrearReglaBase(DtoReglaBaseUpdate datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existente = await _db.reglaBasePuntos.ObtenerReglaAsync();
            if (existente is not null)
                return BadRequest(new { message = "Ya existe una regla base." });

            var regla = new ReglaBasePuntos { Cantidad = datos.Cantidad, Activo = datos.Activo };
            await _db.reglaBasePuntos.Crear(regla);
            await _db.SaveUnitWork();

            return Created("", new { message = "Regla base creada", regla.Id });
        }

        [HttpPut("config/reglabase")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ActualizarReglaBase(DtoReglaBaseUpdate datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var regla = await _db.reglaBasePuntos.ObtenerReglaAsync();
            if (regla is null)
                return NotFound(new { message = "Regla base no encontrada" });

            regla.Cantidad = datos.Cantidad;
            regla.Activo   = datos.Activo;

            await _db.SaveUnitWork();

            return Ok(new { message = "Regla base actualizada" });
        }

        // ─── Configuración: Aceleradores ──────────────────────────────────────

        [HttpGet("config/aceleradores")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ObtenerAceleradores()
        {
            var lista = await _db.aceleradores.ObtenerTodosAsync();

            return Ok(lista.Select(a => new
            {
                a.Id,
                a.Tipo,
                a.TipoAplicacion,
                a.Cantidad,
                a.UmbralMonto,
                a.HoraInicio,
                a.HoraFin,
                a.Activo
            }));
        }

        [HttpPut("config/aceleradores/{id:int}")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> ActualizarAcelerador(int id, DtoAceleradorUpdate datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var acelerador = await _db.aceleradores.FindByIdAsync(id);
            if (acelerador is null)
                return NotFound(new { message = "Acelerador no encontrado" });

            acelerador.Cantidad   = datos.Cantidad;
            acelerador.Activo     = datos.Activo;

            if (acelerador.Tipo == TipoAcelerador.HoraValle)
            {
                if (datos.HoraInicio is null || datos.HoraFin is null)
                    return BadRequest(new { message = "HoraInicio y HoraFin son obligatorios para el acelerador HoraValle" });

                acelerador.HoraInicio = datos.HoraInicio;
                acelerador.HoraFin    = datos.HoraFin;
            }

            await _db.SaveUnitWork();

            return Ok(new { message = "Acelerador actualizado" });
        }

        // ─── Ajuste manual de puntos (solo Admin) ────────────────────────────

        [HttpPost("cliente/{clienteId:int}/ajuste")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> AjusteManual(int clienteId, DtoAjusteManualPuntos datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cliente = await _db.clientes.FindByIdAsync(clienteId);
            if (cliente is null)
                return NotFound(new { message = "Cliente no encontrado" });

            cliente.AgregarPuntos(datos.Cantidad);

            await _db.historialPuntos.Crear(new HistorialPuntos
            {
                Id_Cliente    = clienteId,
                CodigoVenta   = "AJUSTE-MANUAL",
                PuntosBase    = datos.Cantidad,
                PuntosFinales = datos.Cantidad,
                Desglose      = $"Ajuste manual: {datos.Motivo}",
                Fecha         = DateTime.UtcNow
            });

            await _db.SaveUnitWork();

            return Ok(new { message = $"{datos.Cantidad} puntos agregados al cliente", PuntosActuales = cliente.Puntos });
        }
    }
}
