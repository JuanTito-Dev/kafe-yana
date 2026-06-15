namespace KafeYana.Infrastructure.Configuration
{
    /// <summary>
    /// Datos fijos de la empresa emisora para facturación SIAT.
    /// </summary>
    public class DatosEmpresaOptions
    {
        public const string SeccionNombre = "DatosEmpresa";

        public string RazonSocial { get; set; } = string.Empty;

        public string Municipio { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public string CodigoActividad { get; set; } = string.Empty;

        public string ActividadEconomica { get; set; } = string.Empty;
    }
}
