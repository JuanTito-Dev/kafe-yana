using KafeYana.Application.Dtos.CajaDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class CajaController(IUnitWork _db) : ControllerBase
    {
        [HttpPost("Abrir")]
        public async Task<IActionResult> Crear(DtoAbrir datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existeCaja = await _db.cajas.ExisteCaja();
            if (existeCaja)
                return BadRequest(new { message = "Ya existe una caja registrada" });

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            var numeroCaja = await _db.cajahistorial.ContarHistorial() + 1;

            var caja = new Caja
            {
                Nombre = $"CAJ-{numeroCaja:D3}",
                SaldoInicial = datos.SaldoInicial,
                FechaApertura = DateTime.UtcNow,
                AbiertaPor = nombreUsuario
            };

            await _db.cajas.Crear(caja);
            await _db.SaveUnitWork();

            return Ok(new { message = "Caja abierta correctamente", Nombre = caja.Nombre });
        }

        [HttpPost("movimiento")]
        public async Task<IActionResult> Movimiento(DtoCajaMovimiento datos, bool entrada = true)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var caja = await _db.cajas.ObtenerCaja();
            if (caja is null)
                return BadRequest(new { message = "No hay una caja abierta" });

            CajaMovimiento movimiento;

            if (entrada)
                movimiento = caja.CajaIngreso(datos.Cantidad, datos.Categoria, datos.Concepto, datos.Referencia, datos.Nota);
            else
                movimiento = caja.CajaEgresos(datos.Cantidad, datos.Categoria, datos.Concepto, datos.Referencia, datos.Nota);

            await _db.cajamovimientos.Crear(movimiento);
            await _db.SaveUnitWork();

            return Ok(new
            {
                message = entrada ? "Ingreso registrado correctamente" : "Egreso registrado correctamente"
            });
        }


        [HttpPost("cerrar")]
        public async Task<IActionResult> CerrarCaja(DtoCerrarCaja datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            var caja = await _db.cajas.ObtenerCajaConMovimientos();
            if (caja is null)
                return BadRequest(new { message = "No hay una caja abierta" });

            var hayMesasOcupadas = await _db.mesas.HayMesasOcupadas();
            if (hayMesasOcupadas)
                return BadRequest(new { message = "No puedes cerrar la caja, hay mesas con pedidos activos" });

            var hayPedidoLlevar = await _db.parallevar.TienePedidoActivo();
            if (hayPedidoLlevar)
                return BadRequest(new { message = "No puedes cerrar la caja, hay un pedido para llevar activo" });

            var historial = caja.CerrarCaja(datos.MontoFinal, nombreUsuario, datos.Nota);

            await _db.cajahistorial.Crear(historial);
            await _db.cajas.Remove(caja);
            await _db.SaveUnitWork();

            return Ok(new
            {
                message = "Caja cerrada"
            });
        }
    }
}
