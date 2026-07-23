using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoVentaPedido : IValidatableObject
    {
        [Required]
        public required int Id_Pedido { get; set; }

        /// <summary>Cliente registrado. Si no se envía, son obligatorios Nombre y Dni (C.L.) al facturar.</summary>
        public int? Id_Cliente { get; set; }

        [Required]
        public required DtoPagos Pagos { get; set; }

        /// <summary>
        /// Bloque de pagos consolidado para emitir al SIAT cuando se usó
        /// división de cuenta (varios métodos) pero la factura debe mostrar
        /// un único método predominante con el monto total.
        ///
        /// Reglas:
        ///   - Si se envía con exactamente 1 línea y monto > 0, su
        ///     <c>CodigoMetodoPago</c> se usa como <c>Venta.CodigoMetodoPago</c>
        ///     (campo XML al SIAT), ignorando el desempate por mayor monto
        ///     que el backend aplicaría sobre <see cref="Pagos"/>.
        ///   - Si es <c>null</c> o tiene 0/N líneas, se mantiene el
        ///     comportamiento legacy: <c>Venta.CodigoMetodoPago</c> = la línea
        ///     de mayor monto de <see cref="Pagos"/>.
        ///
        /// NO afecta <c>VentaPagos</c> ni <c>Caja</c>: ambos siguen leyendo
        /// <see cref="Pagos"/>.<c>Lineas</c> (el split original se conserva
        /// para auditoría interna y para los acumuladores de caja).
        /// </summary>
        public DtoPagos? PagosFactura { get; set; }

        /// <summary>Si es true, aplica el mejor descuento permanente disponible. Default: false.</summary>
        public bool AplicarDescuentos { get; set; } = false;

        /// <summary>
        /// Si es true, genera factura electrónica y envía al SIAT.
        /// Si es false, solo registra el cobro sin facturación tributaria.
        /// </summary>
        public bool Factura { get; set; } = true;

        /// <summary>
        /// Paramétrica SIAT codigoTipoDocumentoIdentidad (1..N según catálogo SIN vigente).
        /// Obligatorio solo si Factura=true. La validación contra el catálogo vigente
        /// (sincronizado por <c>SincronizadorCatTipoDocumentoIdentidad</c>) la hace
        /// <see cref="Validate"/> vía <see cref="TipoDocumentoIdentidadSiatCatalogo.EsValido"/>:
        /// antes del primer sync acepta 1..5 (fallback), después cualquier código
        /// que el SIN publique.
        /// </summary>
        public int? CodigoTipoDocumento { get; set; }

        /// <summary>Nombre del comprador. Obligatorio solo si no se envía Id_Cliente al facturar.</summary>
        public string? Nombre { get; set; }

        /// <summary>Cédula de identidad (C.L.). Obligatoria solo si no se envía Id_Cliente al facturar.</summary>
        public int? Dni { get; set; }

        /// <summary>Complemento SEGIP. Opcional; null si no aplica.</summary>
        [MaxLength(10, ErrorMessage = "El complemento no puede exceder 10 caracteres.")]
        public string? Complemento { get; set; }

        /// <summary>
        /// Sucursal donde se realiza el cobro, declarada en PuntosVentaSiat.
        /// Si se envía (junto con CodigoPuntoVenta), el backend valida que el
        /// (sucursal, puntoVenta) exista y esté activo, y lo usa para construir
        /// el CUF/CUFD/sobre SOAP de forma consistente.
        /// Si NO se envía, el backend cae al comportamiento legacy:
        /// primer PuntosVentaSiat activo, o appsettings si no hay ninguno.
        /// </summary>
        public int? CodigoSucursal { get; set; }

        /// <summary>
        /// Punto de venta donde se realiza el cobro, declarado en PuntosVentaSiat.
        /// Mismas reglas que <see cref="CodigoSucursal"/>: si viene del frontend
        /// se valida contra BD; si no, fallback automático.
        /// </summary>
        public int? CodigoPuntoVenta { get; set; }

        /// <summary>
        /// Código de país de origen del documento del cliente (catálogo SIN
        /// <c>CatPaisOrigen.Codigo</c>, 1..211). Requerido sólo cuando
        /// <see cref="CodigoTipoDocumento"/> ∈ {2 (CEX), 3 (PAS)} y NO se
        /// envía <see cref="Id_Cliente"/>. El backend lo valida contra la
        /// tabla <c>CatPaisOrigen</c> y lo persiste como FK
        /// <c>Cliente.IdPaisOrigen</c>.
        ///
        /// Si se envía <see cref="Id_Cliente"/> (cliente del dropdown), este
        /// campo se IGNORA: el país se lee de la BD (cliente ya persistido).
        /// </summary>
        public int? CodigoPaisOrigen { get; set; }

        /// <summary>Items parciales a cobrar (producto + cantidad, FIFO across rondas). Si viene con items, es una sub-venta (cobro parcial).</summary>
        public List<DtoItemProductoCobrar>? ItemsCubiertos { get; set; }

        /// <summary>Si true, la mesa queda abierta tras el cobro (pago parcial). Default false.</summary>
        public bool MantenerMesaAbierta { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Factura)
            {
                if (CodigoTipoDocumento is null)
                {
                    yield return new ValidationResult(
                        "El código de tipo de documento es requerido cuando Factura es true.",
                        [nameof(CodigoTipoDocumento)]);
                }
                else if (!TipoDocumentoIdentidadSiatCatalogo.EsValido(CodigoTipoDocumento.Value))
                {
                    yield return new ValidationResult(
                        "El código de tipo de documento no es válido. Valores permitidos según catálogo SIAT vigente (1=CI, 2=CEX, 3=PAS, 4=OD, 5=NIT).",
                        [nameof(CodigoTipoDocumento)]);
                }

                // Cliente extranjero (CEX / PAS) sin Id_Cliente → exigir país de origen.
                // Con Id_Cliente el país se lee del cliente persistido (no del body).
                var tipoDoc = CodigoTipoDocumento ?? 0;
                var esExtranjero = tipoDoc is 2 or 3;
                if (esExtranjero && Id_Cliente is null && CodigoPaisOrigen is null)
                {
                    yield return new ValidationResult(
                        "Debe indicar el país de origen del documento para clientes extranjeros (CEX/PAS).",
                        [nameof(CodigoPaisOrigen)]);
                }

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

                if (!string.IsNullOrWhiteSpace(Nombre) && Dni is > 0)
                    yield break;
            }

            // CodigoSucursal y CodigoPuntoVenta deben venir juntos o ninguno.
            // Si viene solo uno, es un error del frontend (no adivinar el resto).
            if ((CodigoSucursal is null) != (CodigoPuntoVenta is null))
            {
                yield return new ValidationResult(
                    "Debe enviar CodigoSucursal y CodigoPuntoVenta juntos, o ninguno.",
                    [nameof(CodigoSucursal), nameof(CodigoPuntoVenta)]);
            }

            // Si vienen, validar rangos razonables (BD check es en VentaServices).
            if (CodigoSucursal is int suc && suc < 0)
            {
                yield return new ValidationResult(
                    "CodigoSucursal no puede ser negativo.",
                    [nameof(CodigoSucursal)]);
            }

            if (CodigoPuntoVenta is int pv && pv < 0)
            {
                yield return new ValidationResult(
                    "CodigoPuntoVenta no puede ser negativo.",
                    [nameof(CodigoPuntoVenta)]);
            }
            else if (CodigoTipoDocumento is int tipo
                     && !TipoDocumentoIdentidadSiatCatalogo.EsValido(tipo))
            {
                yield return new ValidationResult(
                    "El código de tipo de documento no es válido. Valores permitidos según catálogo SIAT vigente (1=CI, 2=CEX, 3=PAS, 4=OD, 5=NIT).",
                    [nameof(CodigoTipoDocumento)]);
            }
        }
    }
}
