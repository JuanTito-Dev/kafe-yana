using KafeYana.Domain.Entities.Facturacion;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IVerificaNitService
    {
        Task<VerificaNitResult> VerificarNitAsync(
            long nit,
            int codigoSucursal,
            int codigoPuntoVenta = 0,
            CancellationToken ct = default);
    }
}
