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
                    var opcionesPorDetalle = new List<(int DetalleIndex, int IdOpcion)>();

                    for (int i = 0; i < detallesDto.Count; i++)
                    {
                        var detalleDto = detallesDto[i];

                        var producto = await _unitWork.productos.FindByIdAsync(detalleDto.Id_Producto);
                        if (producto is null)
                            throw new DetalleRondaException($"El producto con ID {detalleDto.Id_Producto} no existe.");

                        var precioAjuste = 0.00M;

                        if (detalleDto.Ids_Opcion != null && detalleDto.Ids_Opcion.Count > 0)
                        {
                            foreach (var idOpcion in detalleDto.Ids_Opcion)
                            {
                                bool opcionValida = await _unitWork.opciones.Opciondeproducto(detalleDto.Id_Producto, idOpcion);
                                if (!opcionValida)
                                    throw new OpcionProductoException($"La opción {idOpcion} no pertenece al producto {detalleDto.Id_Producto}.");

                                var opcion = await _unitWork.opciones.FindByIdAsync(idOpcion);
                                if (opcion is null)
                                    throw new OpcionProductoException($"La opción con ID {idOpcion} no existe.");

                                precioAjuste += opcion.AjustePrecio;
                                opcionesPorDetalle.Add((i, idOpcion));
                            }
                        }

                        var detalle = new Detalle_ronda
                        {
                            Id_Producto = producto.Id,
                            Nombre_Producto = producto.Nombre,
                            Cantidad = detalleDto.Cantidad,
                            Precio = producto.Precio + precioAjuste
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

                    await _unitWork.rondas.Crear(ronda);
                    await _unitWork.SaveUnitWork();

                    var detallesGuardados = await _db.Detalle_Rondas
                        .Where(d => d.Id_Ronda == ronda.Id)
                        .OrderBy(d => d.Id)
                        .ToListAsync();

                    foreach (var (detalleIndex, idOpcion) in opcionesPorDetalle)
                    {
                        await _db.Database.ExecuteSqlAsync(
                            @$"INSERT INTO ""Detalle_Ronda_Opcion"" (""Id_Detalle_Ronda"", ""Id_Opcion"")
                               VALUES ({detallesGuardados[detalleIndex].Id}, {idOpcion})");
                    }

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