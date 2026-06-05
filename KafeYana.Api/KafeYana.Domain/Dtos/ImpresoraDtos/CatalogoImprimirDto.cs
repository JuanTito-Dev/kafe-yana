namespace KafeYana.Application.Dtos.ImpresoraDtos
{
    public class CatalogoImprimirDto
    {
        public List<ProductoCatalogoDto> Productos { get; set; } = [];
    }

    public class ProductoCatalogoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
    }
}
