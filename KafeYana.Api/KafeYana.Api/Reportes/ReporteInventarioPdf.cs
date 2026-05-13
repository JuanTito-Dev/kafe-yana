using KafeYana.Application.Dtos.ReporteDtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KafeYana.Api.Reportes
{
    public class ReporteInventarioPdf(DtoReporteInventario datos) : IDocument
    {
        // ── Colores de la imagen ────────────────────────────────────────────────
    private static readonly string ColorCafe    = "#7B3F00";
    private static readonly string ColorCabecer = "#8B4513";
    private static readonly string ColorFilaAlt = "#F5E6D3";
    private static readonly string ColorBlanco  = "#FFFFFF";

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        // ── Header ──────────────────────────────────────────────────────────────
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("Resumen del inventario")
                    .FontSize(16).Bold().FontColor(ColorCafe);

                row.ConstantItem(200)
                    .AlignRight()
                    .Text($"Generado: {datos.GeneradoEn:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        // ── Contenido ───────────────────────────────────────────────────────────
        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(16);

                col.Item().Element(ComposeResumen);
                col.Item().Element(ComposeDistribucion);
                col.Item().Element(ComposeStockBajoCritico);
                col.Item().Element(ComposeProximosAgotarse);
            });
        }

        // ── Sección 1: Resumen ──────────────────────────────────────────────────
        private void ComposeResumen(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                    });

                    // Encabezado
                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Métrica");
                        h.Cell().Element(CeldaCabecera).Text("Valor");
                    });

                    // Filas
                    FilaResumen(table, "Total productos",     datos.Resumen.TotalProductos.ToString(), true);
                    FilaResumen(table, "Total insumos",       datos.Resumen.TotalInsumos.ToString(), false);
                    FilaResumen(table, "Ítems con stock bajo",datos.Resumen.ItemsConStockBajo.ToString(), true);
                    FilaResumen(table, "Valor del inventario",$"Bs {datos.Resumen.ValorInventario:N2}", false);
                });
            });
        }

        private static void FilaResumen(TableDescriptor table, string metrica, string valor, bool alt)
        {
            var bg = alt ? ColorFilaAlt : ColorBlanco;

            table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
                .Text(metrica).FontColor(ColorCafe);

            table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
                .AlignRight().Text(valor).FontColor(ColorCafe);
        }

        // ── Sección 2: Distribución por categoría ───────────────────────────────
        private void ComposeDistribucion(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Distribución por categoría").Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(6);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Categoría");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Productos");
                    });

                    bool alt = false;
                    foreach (var cat in datos.DistribucionPorCategoria)
                    {
                        var bg = alt ? ColorFilaAlt : ColorBlanco;
                        alt = !alt;

                        table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
                            .Text(cat.Categoria).FontColor(ColorCafe);

                        table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
                            .AlignRight().Text(cat.TotalProductos.ToString()).FontColor(ColorCafe);
                    }
                });
            });
        }

        // ── Sección 3: Stock bajo crítico ────────────────────────────────────────
        private void ComposeStockBajoCritico(IContainer container)
        {
            var titulo = $"Stock bajo crítico ({datos.StockBajoCritico.Count} ítems)";
            container.Column(col =>
            {
                col.Item().Text(titulo).Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(6);
                col.Item().Element(c => TablaItems(c, datos.StockBajoCritico));
            });
        }

        // ── Sección 4: Próximos a agotarse ───────────────────────────────────────
        private void ComposeProximosAgotarse(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Top 10 — Próximos a agotarse").Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(6);
                col.Item().Element(c => TablaItemsNumerada(c, datos.ProximosAgotarse));
            });
        }

        // ── Tabla compartida de ítems (sin número) ───────────────────────────────
        private static void TablaItems(IContainer container, List<DtoItemStockBajo> items)
        {
            container.Table(table =>
            {
                DefinirColumnasItems(table, conNumero: false);

                table.Header(h =>
                {
                    h.Cell().Element(CeldaCabecera).Text("Tipo");
                    h.Cell().Element(CeldaCabecera).Text("Nombre");
                    h.Cell().Element(CeldaCabecera).Text("Categoría");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Stock");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Mínimo");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Ratio");
                });

                bool alt = false;
                foreach (var item in items)
                {
                    var bg = alt ? ColorFilaAlt : ColorBlanco;
                    alt = !alt;

                    FilaItem(table, item, bg);
                }
            });
        }

        // ── Tabla con número (#) para el Top 10 ──────────────────────────────────
        private static void TablaItemsNumerada(IContainer container, List<DtoItemStockBajo> items)
        {
            container.Table(table =>
            {
                DefinirColumnasItems(table, conNumero: true);

                table.Header(h =>
                {
                    h.Cell().Element(CeldaCabecera).Text("#");
                    h.Cell().Element(CeldaCabecera).Text("Nombre");
                    h.Cell().Element(CeldaCabecera).Text("Categoría");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Stock");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Mínimo");
                    h.Cell().Element(CeldaCabecera).AlignRight().Text("Ratio");
                });

                bool alt = false;
                int i = 1;
                foreach (var item in items)
                {
                    var bg = alt ? ColorFilaAlt : ColorBlanco;
                    alt = !alt;

                    table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                        .Text(i.ToString()).FontColor(ColorCafe);

                    FilaItemSinTipo(table, item, bg);
                    i++;
                }
            });
        }

        private static void DefinirColumnasItems(TableDescriptor table, bool conNumero)
        {
            table.ColumnsDefinition(c =>
            {
                if (conNumero) c.ConstantColumn(20);
                else           c.ConstantColumn(50);  // Tipo
                c.RelativeColumn(3);  // Nombre
                c.RelativeColumn(4);  // Categoría
                c.RelativeColumn(2);  // Stock
                c.RelativeColumn(2);  // Mínimo
                c.ConstantColumn(35); // Ratio
            });
        }

        private static void FilaItem(TableDescriptor table, DtoItemStockBajo item, string bg)
        {
            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .Text(item.Tipo).FontColor(ColorCafe);

            FilaItemSinTipo(table, item, bg);
        }

        private static void FilaItemSinTipo(TableDescriptor table, DtoItemStockBajo item, string bg)
        {
            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .Text(item.Nombre).FontColor(ColorCafe);

            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .Text(item.Categoria).FontColor(ColorCafe);

            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .AlignRight().Text(item.Stock).FontColor(ColorCafe);

            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .AlignRight().Text(item.Minimo).FontColor(ColorCafe);

            var ratioColor = item.Ratio <= 10 ? Colors.Red.Medium
                           : item.Ratio <= 50  ? Colors.Orange.Medium
                           : ColorCafe;

            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                .AlignRight().Text($"{item.Ratio}%").FontColor(ratioColor);
        }

        // ── Footer ──────────────────────────────────────────────────────────────
        private static void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        }

        // ── Helper celda de cabecera ─────────────────────────────────────────────
        private static IContainer CeldaCabecera(IContainer container)
        {
            return container
                .Background(ColorCabecer)
                .PaddingHorizontal(6)
                .PaddingVertical(5);
        }
    }
}
