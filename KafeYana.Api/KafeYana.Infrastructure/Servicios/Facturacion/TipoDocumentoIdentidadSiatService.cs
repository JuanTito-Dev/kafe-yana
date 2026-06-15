using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class TipoDocumentoIdentidadSiatService
    {
        public static bool EsValido(int codigo) =>
            Enum.IsDefined(typeof(TipoDocumentoIdentidadSiat), codigo);

        public static string ObtenerDescripcion(int codigo) =>
            TipoDocumentoIdentidadSiatDescripciones.PorCodigo.TryGetValue(codigo, out var descripcion)
                ? descripcion
                : string.Empty;
    }
}
