using KafeYana.Application.IRepositorio;
using KafeYana.Application.Exceptions;
using KafeYana.Domain.Dtos.Detalle_RondaDtos;
using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities.Inventario;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Servicios
{
    public class Detalle_RondaService
    {
        private readonly IUnitWork _unitWork;
        private readonly AppDbContext _db;

        public Detalle_RondaService(IUnitWork unitWork, AppDbContext db)
        {
            _unitWork = unitWork;
            _db = db;
        }

        public async Task<Ronda> CrearRondaConDetallesAsync(int idPedido, List<DtoRondadetalle> detallesDto)
        {
            _db.ChangeTracker.Clear();

            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var listaDetalles = new List<Detalle_ronda>();
                    var subtotal = 0.00M;
                    

                    foreach (var i in detallesDto)
                    {
                        var opcionesPorDetalle = new List<Detalle_Ronda_Opcion>();
                        var producto = await _unitWork.productos.FindByIdAsync(i.Id_Producto);
                        if (producto is null)
                            throw new DetalleRondaException($"El producto con ID {i.Id_Producto} no existe.");

                        var precioAjuste = 0.00M;

                        if (i.Ids_Opcion != null && i.Ids_Opcion.Count > 0)
                        {
                            foreach (var idOpcion in i.Ids_Opcion)
                            {
                                bool opcionValida = await _unitWork.opciones.Opciondeproducto(i.Id_Producto, idOpcion);
                                if (!opcionValida)
                                    throw new OpcionProductoException($"La opción {idOpcion} no pertenece al producto {i.Id_Producto}.");

                                var opcion = await _unitWork.opciones.FindByIdAsync(idOpcion);
                                if (opcion is null)
                                    throw new OpcionProductoException($"La opción con ID {idOpcion} no existe.");

                                precioAjuste += opcion.AjustePrecio;

                                var detalleopcion = new Detalle_Ronda_Opcion(idOpcion, producto.Precio.ToString(), opcion.AjustePrecio);
                                opcionesPorDetalle.Add(detalleopcion);
                            }
                        }

                        var detalle = new Detalle_ronda
                        {
                            Id_Producto = producto.Id,
                            Nombre_Producto = producto.Nombre,
                            Cantidad = i.Cantidad,
                            Precio = producto.Precio + precioAjuste,
                            Opciones = opcionesPorDetalle
                            
                        };

                        subtotal += detalle.Precio * detalle.Cantidad;
                        listaDetalles.Add(detalle);
                    }

                    if (listaDetalles.Count == 0)
                        throw new DetalleRondaException("No se han agregado productos a la ronda.");

                    var numeroRonda = await _unitWork.rondas.Count(x => x.Id_Pedido == idPedido) + 1;

                    var ronda = new Ronda
                    {
                        Id_Pedido = idPedido,
                        Ronda_Descripcion = $"Ronda {numeroRonda}",
                        Detalle = listaDetalles,
                        SubTotal = subtotal
                    };

                    await transaction.CommitAsync();

                    return ronda;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}