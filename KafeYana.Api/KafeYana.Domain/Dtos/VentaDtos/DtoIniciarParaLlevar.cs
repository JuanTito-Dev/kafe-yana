using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoIniciarParaLlevar : IValidatableObject
    {
        /// <summary>Cliente registrado. Si no se envía, son obligatorios Nombre y Dni (C.L.).</summary>
        public int? Id_Cliente { get; set; }

        /// <summary>Nombre del cliente. Obligatorio solo si no se envía Id_Cliente.</summary>
        public string? Nombre { get; set; }

        /// <summary>Cédula de identidad (C.L.). Obligatoria solo si no se envía Id_Cliente.</summary>
        public int? Dni { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Id_Cliente is int idCliente)
            {
                if (idCliente <= 0)
                {
                    yield return new ValidationResult(
                        "Id_Cliente debe ser mayor a cero.",
                        [nameof(Id_Cliente)]);
                }

                yield break;
            }

            if (string.IsNullOrWhiteSpace(Nombre) && Dni is null)
                yield break;

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                yield return new ValidationResult(
                    "El nombre es obligatorio cuando no se envía Id_Cliente.",
                    [nameof(Nombre)]);
            }

            if (Dni is null or <= 0)
            {
                yield return new ValidationResult(
                    "La C.L. es obligatoria cuando no se envía Id_Cliente.",
                    [nameof(Dni)]);
            }
        }
    }
}
