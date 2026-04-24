using KafeYana.Application.IRepositorio;
using KafeYana.Application.Exceptions;
using KafeYana.Domain.Dtos.Detalle_RondaDtos;
using KafeYana.Domain.Entities.Inventario;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KafeYana.Infrastructure.Data;
using KafeYana.Domain.Dtos.RondaDtos;

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

        /// <summary>
        /// Crea una ronda con detalles y opciones. Todo se guarda o nada.
        /// </summary>
        public async Task<Ronda> CrearRondaConDetallesAsync(int idPedido, List<DtoRondadetalle> detallesDto)
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var listaDetalles = new List<Detalle_ronda>();
                    var subtotal = 0.00M;

                    // Procesar cada detalle
                    foreach (var detalleDto in detallesDto)
                    {
                        // Validar producto existe
                        var producto = await _unitWork.productos.FindByIdAsync(detalleDto.Id_Producto);
                        if (producto is null)
                            throw new DetalleRondaException($"El producto con ID {detalleDto.Id_Producto} no existe.");

                        // Validar que no exista detalle duplicado
                        bool existe = await _unitWork.detallesRondas.ExisteDetalleRondaPorProductoAsync(idPedido, detalleDto.Id_Producto);
                        if (existe)
                            throw new DetalleRondaException($"Ya existe un detalle para el producto {detalleDto.Id_Producto} en esta ronda.");

                        var precioAjuste = 0.00M;

                        // Procesar opciones si existe
                        var opciones = new List<DtoDetalle_RondaOpcionCrear>();

                        if (detalleDto.Id_Opcion.HasValue && detalleDto.Id_Opcion > 0)
                        {
                            // Validar que la opción pertenece al producto
                            bool opcionValida = await _unitWork.opciones.Opciondeproducto(detalleDto.Id_Producto, detalleDto.Id_Opcion.Value);
                            if (!opcionValida)
                                throw new OpcionProductoException($"La opción {detalleDto.Id_Opcion} no pertenece al producto {detalleDto.Id_Producto}.");

                            var opcion = await _unitWork.opciones.FindByIdAsync(detalleDto.Id_Opcion.Value);
                            if (opcion is null)
                                throw new OpcionProductoException($"La opción con ID {detalleDto.Id_Opcion} no existe.");

                            precioAjuste = opcion.AjustePrecio;
                            opciones.Add(new DtoDetalle_RondaOpcionCrear { Id_Opcion = detalleDto.Id_Opcion.Value });
                        }

                        var detalle = new Detalle_ronda
                        {
                            Id_Producto = producto.Id,
                            Nombre_Producto = producto.Nombre,
                            Cantidad = detalleDto.Cantidad,
                            Precio = producto.Precio + precioAjuste,
                            Opciones = new List<Detalle_Ronda_Opcion>()
                        };

                        // Agregar opciones al detalle
                        foreach (var opcionDto in opciones)
                        {
                            detalle.Opciones.Add(new Detalle_Ronda_Opcion
                            {
                                Id_Opcion = opcionDto.Id_Opcion
                            });
                        }

                        subtotal += detalle.Precio * detalle.Cantidad;
                        listaDetalles.Add(detalle);
                    }

                    if (listaDetalles.Count == 0)
                        throw new DetalleRondaException("No se han agregado productos a la ronda.");

                    // Obtener número de ronda
                    var numeroRonda = await _unitWork.rondas.Count(x => x.Id_Pedido == idPedido) + 1;

                    // Crear ronda
                    var ronda = new Ronda
                    {
                        Id_Pedido = idPedido,
                        Ronda_Descripcion = $"Ronda {numeroRonda}",
                        Detalle = listaDetalles,
                        SubTotal = subtotal
                    };

                    // Guardar todo
                    await _unitWork.rondas.Crear(ronda);
                    await _unitWork.SaveUnitWork();
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