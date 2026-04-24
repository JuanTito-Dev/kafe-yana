using KafeYana.Application.Dtos.AjusteStockDtos;
using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Net;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AjusteStockController(IUnitWork _db) : ControllerBase
    {
        [HttpPost("Comprado")]
        public async Task<IActionResult> AjusteComprado(DtoSotck_AjusteCrear datos, bool entrada = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var comprado = await _db.productos.GetCompradowithproducto(datos.Id);

            var nombre = User.Identity?.Name;

            if (nombre is null) return BadRequest("Usuario no encontrado");

            if (comprado is null) return BadRequest("Producto no encontrado");

            if (datos.Cantidad > comprado.Stock_actual && !entrada) return BadRequest("No puedes quitar mas productos del que tienes");

            var ajusteInfo = new Stock_Ajuste
            {
                Nombre = comprado.Producto.Nombre,
                Tipo = comprado.Producto.Tipo,
                Usuario = nombre,
                Perdida = 0
            };
            ajusteInfo.StockAnterior = comprado.Stock_actual;

            comprado.Stock_actual += entrada ? datos.Cantidad : (datos.Cantidad * -1);

            ajusteInfo.StockNuevo = comprado.Stock_actual;
            ajusteInfo.Ajuste = ajusteInfo.StockNuevo - ajusteInfo.StockAnterior;

            if (!entrada) ajusteInfo.Perdida = comprado.Costo_compra * datos.Cantidad;

            ajusteInfo.Nota = datos.Nota;
            ajusteInfo.Motivo = datos.Motivo;
            ajusteInfo.Fecha = DateTime.UtcNow;

            await _db.ajustes.Crear(ajusteInfo);

            await _db.SaveUnitWork();

            return Ok(new { message = "Ajuste registrado" });
        }

        [HttpPost("Insumo")]
        public async Task<IActionResult> InsumoUpdate(DtoSotck_AjusteCrear datos, bool entrada = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var Insumo = await _db.insumos.FindByIdAsync(datos.Id);

            var nombre = User.Identity?.Name;

            if (Insumo is null) return BadRequest("Inusmo no encontrado");

            if (nombre == null) return BadRequest("Usuario no encontrado");

            if (datos.Cantidad > Insumo.Stock_actual && !entrada) return BadRequest("No puedes quitar mas insumos del que tienes");

            var AjusteInfo = new Stock_Ajuste
            {
                Nombre = Insumo.Nombre,
                Tipo = "Insumo",
                Usuario = nombre,
                Perdida = 0
            };

            AjusteInfo.StockAnterior = Insumo.Stock_actual;
            Insumo.Stock_actual += entrada ? datos.Cantidad : (datos.Cantidad * -1);
            AjusteInfo.StockNuevo = Insumo.Stock_actual;
            AjusteInfo.Ajuste = AjusteInfo.StockNuevo - AjusteInfo.StockAnterior;

            if (!entrada) AjusteInfo.Perdida = Insumo.Costo * datos.Cantidad;

            AjusteInfo.Nota = datos.Nota;
            AjusteInfo.Motivo = datos.Motivo;
            AjusteInfo.Fecha = DateTime.UtcNow;

            await _db.ajustes.Crear(AjusteInfo);

            await _db.SaveUnitWork();

            return Ok(new { message = "Ajuste registrado" });
        }

        [HttpPost("Elaborado")]
        public async Task<IActionResult> AjusteElaborado(DtoAjusteStockElaborado datos, bool entrada = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var elaborado = await _db.elaborados.ElaboradoWithProducto(datos.Id_elaborado);

            if (elaborado is null) return BadRequest("Producto no encontrado");

            if (entrada && !elaborado.Producible) return BadRequest("El producto no es producible");

            var nombre = User.Identity?.Name;

            if (nombre is null) return BadRequest("Usuario no encontrado");

            var AjusteInfo = datos.Crear(elaborado.Producto.Nombre, nombre);

            if (!entrada && elaborado.Producible)
            {
                if (datos.Cantidad > elaborado.Stock_actual) return BadRequest("No puedes quitar mas productos del que tienes");
            }

            AjusteInfo.StockAnterior = elaborado.Stock_actual;
            if (elaborado.Producible) elaborado.Stock_actual += entrada ? datos.Cantidad : (datos.Cantidad * (-1));
            AjusteInfo.StockNuevo = elaborado.Stock_actual;
            AjusteInfo.Ajuste = AjusteInfo.StockNuevo - AjusteInfo.StockAnterior;
            if (!entrada) AjusteInfo.Perdida = elaborado.Producto.Precio * datos.Cantidad;
            
            if (!elaborado.Producible)
            {
                var receta = await _db.recetas.GetRectaByIdElaborado(elaborado.Id);
                RestarInsumo(receta, datos.Cantidad);
                
            }

            await _db.ajustes.Crear(AjusteInfo);

            await _db.SaveUnitWork();

            return Ok(new { message = "Ajuste registrado" });
        }

        private void RestarInsumo(Receta? receta, int cantidad)
        {
            if (receta is null) return;

            foreach (var detalle in receta.Detalles)
            {
                detalle.Insumo.Stock_actual -= (int)(detalle.Cantidad * cantidad * (1 + (detalle.Merma / 100)));
            }
        }
    }
}
