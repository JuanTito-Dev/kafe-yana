using KafeYana.Application.Dtos.ReferidosDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReferidosController(IUnitWork _db) : ControllerBase
    {
        /// <summary>Fila única de configuración (creada por seed). Solo lectura.</summary>
        [HttpGet("config")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> ObtenerConfig()
        {
            var cfg = await _db.referidosConfig.ObtenerUnicaAsync();
            if (cfg is null)
                return NotFound(new { message = "Configuración de referidos no inicializada" });

            return Ok(new
            {
                cfg.Id,
                cfg.PuntosReferidor,
                cfg.PuntosReferido,
                cfg.Activo
            });
        }

        /// <summary>Actualiza puntos y estado activo. No hay alta manual ni borrado.</summary>
        [HttpPut("config")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> ActualizarConfig(DtoReferidosConfigUpdate datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cfg = await _db.referidosConfig.ObtenerUnicaAsync();
            if (cfg is null)
                return NotFound(new { message = "Configuración de referidos no inicializada" });

            cfg.PuntosReferidor = datos.PuntosReferidor;
            cfg.PuntosReferido  = datos.PuntosReferido;
            cfg.Activo          = datos.Activo;

            await _db.SaveUnitWork();

            return Ok(new { message = "Configuración de referidos actualizada" });
        }

        /// <summary>Crea el nuevo cliente (referido) con los mismos datos que POST Cliente + IdReferidor. Si el programa está activo, suma puntos según config y registra historial.</summary>
        [HttpPost("cliente")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> CrearClienteReferido(DtoClienteReferidoCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cfg = await _db.referidosConfig.ObtenerUnicaAsync();
            if (cfg is null)
                return NotFound(new { message = "Configuración de referidos no inicializada" });

            if (!cfg.Activo)
                throw new InventarioException("El programa de referidos está inactivo");

            var referidor = await _db.clientes.FindByIdAsync(datos.IdReferidor);
            if (referidor is null || !referidor.Estado)
                throw new InventarioException("Cliente referidor no encontrado o inactivo");

            var nuevo = datos.Adapt<Cliente>();
            nuevo.Correonormalizado = !string.IsNullOrEmpty(datos.Correo)
                ? datos.Correo!.ToUpper()
                : string.Empty;

            await _db.clientes.Crear(nuevo);

            referidor.AgregarPuntos(cfg.PuntosReferidor);
            nuevo.AgregarPuntos(cfg.PuntosReferido);

            await _db.historialReferidos.Crear(new HistorialReferido
            {
                NombreReferidor = referidor.Nombre,
                NombreReferido  = nuevo.Nombre,
                PuntosReferidor = cfg.PuntosReferidor,
                PuntosReferido  = cfg.PuntosReferido,
                Fecha           = DateTime.UtcNow
            });

            await _db.SaveUnitWork();

            return Created("", new
            {
                message = "Cliente referido creado",
                nuevo.Id,
                puntosOtorgadosReferidor = cfg.PuntosReferidor,
                puntosOtorgadosReferido  = cfg.PuntosReferido
            });
        }
    }
}
