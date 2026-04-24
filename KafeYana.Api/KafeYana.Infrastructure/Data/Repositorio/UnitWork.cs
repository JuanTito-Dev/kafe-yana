using KafeYana.Application.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class UnitWork : IUnitWork
    {
        private readonly AppDbContext _db;
        public IProductoRepositorio productos { get; private set; }

        public IElaboradoRepositorio elaborados { get; private set; }

        public IAjusteStockRepositorio ajustes { get; private set; }

        public IInsumoRepositorio insumos { get; private set; }

        public IRecetaRepositorio recetas { get; private set; }

        public IMesaRepositorio mesas { get; private set; }

        public IPedidoRepositorio Pedidos { get; private set; }

        public IRondaRepositorio rondas { get; private set; }

        public IDetalle_RondaRepositorio detallesRondas { get; private set; }

        public IClienteRespositorio clientes { get; private set; }
        
        public IOpcionRepositorio opciones { get; private set; }
        
        public IComboRepositorio Combo { get; private set; }
        
        public IVentaRepositorio ventas { get; private set; }

        public UnitWork(AppDbContext db, IProductoRepositorio productos, IElaboradoRepositorio elaborados,
                IInsumoRepositorio insumos,
                IAjusteStockRepositorio ajusteStocks,
                IRecetaRepositorio recetas,
                IMesaRepositorio mesas,
                IPedidoRepositorio pedidos,
                IRondaRepositorio rondas,
                IDetalle_RondaRepositorio detallesRondas,
                IClienteRespositorio clientes,
                IOpcionRepositorio opciones,
                IComboRepositorio combo,
                IVentaRepositorio ventas)
        {
            _db = db;
            this.productos = productos;
            this.elaborados = elaborados;
            this.insumos = insumos;
            ajustes = ajusteStocks;
            this.recetas = recetas;
            this.mesas = mesas;
            Pedidos = pedidos;
            this.rondas = rondas;
            this.detallesRondas = detallesRondas;
            this.clientes = clientes;
            this.opciones = opciones;
            Combo = combo;
            this.ventas = ventas;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public async Task<int> SaveUnitWork()
        {
            return await _db.SaveChangesAsync();
        }
    }
}
