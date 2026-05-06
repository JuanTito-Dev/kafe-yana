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

            if (nombre is null) return NotFound(new { message = "Usuario no encontrado" });

            if (comprado is null) return NotFound(new { message = "Producto no encontrado" });

            if (datos.Cantidad > comprado.Stock_actual && !entrada) return BadRequest(new { message = "No puedes quitar mas productos del que tienes" });

            var (ajuste, movimiento) = entrada
                    ? comprado.AjusteEntrada(nombre, datos.Cantidad, datos.Nota, datos.Motivo)
                    : comprado.AjusteSalida(nombre, datos.Cantidad, datos.Nota, datos.Motivo);

            await _db.ajustes.Crear(ajuste);
            await _db.movimientos.Crear(movimiento);
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

            if (datos.Cantidad > Insumo.Stock_actual && !entrada) return BadRequest(new { message = "No puedes quitar mas insumos del que tienes" });

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
            if (elaborado is null)
                return NotFound(new { message = "Producto no encontrado" });

            if (entrada && !elaborado.Producible)
                return BadRequest(new { message = "Solo se puede registrar entrada a productos producibles" });

            var nombre = User.Identity?.Name;
            if (nombre is null)
                return NotFound(new { message = "Usuario no encontrado" });

            var ajusteInfo = datos.Crear(elaborado.Producto.Nombre, nombre);

            ajusteInfo.StockAnterior = elaborado.Stock_actual;
            var receta = await _db.recetas.GetRectaByIdElaborado(elaborado.Id);
            if (receta is null)
                return NotFound(new { message = "No se encontró receta para este producto" });

            if (entrada)
            {
                // entrada: descuenta porciones completas
                var costo = RestarInsumo(receta, datos.Cantidad * receta.Porciones);
                var movimiento = elaborado.AjusteEntrada(datos.Cantidad, receta.Porciones, ajusteInfo, costo);
                await _db.movimientos.Crear(movimiento);

            }
            else if (elaborado.Producible)
            {
                // Caso 2: Salida producible → solo descontar stock
                if (datos.Cantidad > elaborado.Stock_actual)
                    return BadRequest(new { message = "No puedes quitar más productos de los que tienes" });

                var precio = elaborado.Producto.Precio / receta.Porciones;

                ajusteInfo.Perdida = precio;

                var movimiento = elaborado.AjusteSalida(datos.Cantidad , precio , ajusteInfo);

                await _db.movimientos.Crear(movimiento);
            }
            else
            {
                
                if (receta is null)
                    return NotFound(new { message = "No se encontró receta para este producto" });

                // salida: datos.Cantidad ya son porciones directamente
                var costo = RestarInsumo(receta, datos.Cantidad);
                ajusteInfo.Perdida = costo;
                ajusteInfo.Ajuste = - datos.Cantidad;
                ajusteInfo.StockNuevo = ajusteInfo.StockAnterior - elaborado.Stock_actual;

            }

            
            await _db.ajustes.Crear(ajusteInfo);
            await _db.SaveUnitWork();

            return Ok(new { message = "Ajuste registrado" });
        }

        private decimal RestarInsumo(Receta receta, int porciones)
        {
            var costo = 0.00M;
            foreach (var detalle in receta.Detalles)
            {
                // cantidad proporcional a las porciones que se van a usar
                var cantidadPorPorcion = detalle.Cantidad / receta.Porciones;
                var cantidadFinal = cantidadPorPorcion * porciones * (1 + detalle.Merma / 100);

                var factor = detalle.Insumo.Factor_conversion > 0 ? detalle.Insumo.Factor_conversion : 1;
                costo += (cantidadFinal * detalle.Insumo.Costo) / factor;

                var resta = (int)cantidadFinal;
                detalle.Insumo.Stock_actual -= detalle.Insumo.Stock_actual <= resta
                    ? detalle.Insumo.Stock_actual
                    : resta;
            }
            return costo;
        }
    }
}
