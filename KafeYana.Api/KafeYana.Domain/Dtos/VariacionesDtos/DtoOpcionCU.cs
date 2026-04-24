using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.VariacionesDtos
{
    public class DtoOpcionCU
    {
        [Required(ErrorMessage = "El nombre de la opción es obligatorio.")]
        public required string Nombre { get; set; }

        [Required]
        public required decimal AjustePrecio { get; set; }

        [Required]
        public int Id_variacion { get; set; }

        public List<DtoAjusteCU> Ajustes { get; set; } = new List<DtoAjusteCU>();

        public Opcion Crear()
        {
            return new Opcion
            {
                Nombre = this.Nombre,
                AjustePrecio = this.AjustePrecio,
                Id_variacion = this.Id_variacion,
                Ajustes = Ajustes.Select(a => new Ajuste
                {
                    Id_Insumo = a.Id_Insumo,
                    Cantidad = a.Cantidad,
                    TipoAjuste = a.Id_InsumoNuevo is null
                        ? TiposAjuste.Modificacion
                        : TiposAjuste.Reemplazo,
                    Id_InsumoNuevo = a.Id_InsumoNuevo
                }).ToList()
            };
        }

        public void Actualizar(Opcion existente)
        {
            existente.Nombre = this.Nombre;
            existente.AjustePrecio = this.AjustePrecio;
            // Reemplazo total de ajustes igual que hiciste con detalles}
            existente.Ajustes.Clear();
            existente.Ajustes = Ajustes.Select(a => new Ajuste
            {
                Id_Insumo = a.Id_Insumo,
                Cantidad = a.Cantidad,
                TipoAjuste = a.Id_InsumoNuevo is null
                    ? TiposAjuste.Modificacion
                    : TiposAjuste.Reemplazo,
                Id_InsumoNuevo = a.Id_InsumoNuevo
            }).ToList();
        }
    }
}
