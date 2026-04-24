using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public record DtoRespuestaLista
    {
        public int Actualizados { get; set; }

        public int Creados { get; set; }

        public DtoRespuestaLista(int actualizados, int creados)
        {
            Actualizados = actualizados;
            Creados = creados;
        }
    }
}
