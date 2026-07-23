using System.Globalization;
using KafeYana.Application.Dtos.ReporteDtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KafeYana.Api.Reportes
{
    /// <summary>
    /// PDF del reporte mensual de productos más vendidos / mayor rotación.
    /// </summary>
    public class ReporteProductosMensualPdf(DtoReporteMensualProductos datos) : IDocument
    {
        private static readonly string ColorCafe     = "#7B3F00";
        private static readonly string ColorCabecer  = "#8B4513";
        private static readonly string ColorFilaAlt  = "#F5E6D3";
        private static readonly string ColorBlanco   = "#FFFFFF";

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var nombreMes = CultureInfo.GetCultureInfo("es-ES")
                .DateTimeFormat.GetMonthName(datos.Mes);

            container.Row(row =>
            {
                row.RelativeItem().Text($"Reporte Mensual de Productos — {Capitalize(nombreMes)} {datos.Anio}")
                    .FontSize(16).Bold().FontColor(ColorCafe);

                row.ConstantItem(200)
                    .AlignRight()
                    .Text($"Generado: {datos.GeneradoEn:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(14);

                col.Item().Element(ComposeResumen);
                col.Item().Element(ComposeTopUnidades);
                col.Item().Element(ComposeTopIngresos);
                col.Item().Element(ComposeTopRotacion);
                col.Item().Element(ComposePorCategoria);
            });
        }

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
                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Métrica");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Valor");
                    });

                    FilaResumen(table, "Total facturado",       $"Bs {datos.Resumen.TotalFacturado:N2}", true);
                    FilaResumen(table, "Número de facturas",    datos.Resumen.NumeroFacturas.ToString(), false);
                    FilaResumen(table, "Unidades vendidas",     datos.Resumen.UnidadesVendidas.ToString(), true);
                    FilaResumen(table, "Productos distintos",   datos.Resumen.ProductosDistintos.ToString(), false);
                    FilaResumen(table, "Categorías activas",    datos.Resumen.CategoriasActivas.ToString(), true);
                });
            });
        }

        private void ComposeTopUnidades(IContainer container)
            => ComposeTop(container, "Top 10 — Más unidades vendidas", datos.TopUnidades, mostrarRotacion: false);

        private void ComposeTopIngresos(IContainer container)
            => ComposeTop(container, "Top 10 — Mayor ingreso generado", datos.TopIngresos, mostrarRotacion: false);

        private void ComposeTopRotacion(IContainer container)
            => ComposeTop(container, "Top 10 — Mayor rotación (unidades / stock promedio)", datos.TopRotacion, mostrarRotacion: true);

        private static void ComposeTop(IContainer container, string titulo, List<DtoProductoTop> items, bool mostrarRotacion)
        {
            container.Column(col =>
            {
                col.Item().Text(titulo).Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(4);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(20);
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.ConstantColumn(60);   // Unidades
                        c.ConstantColumn(80);   // Ingresos
                        if (mostrarRotacion)
                            c.ConstantColumn(60);   // Stock prom
                            c.ConstantColumn(60);   // Rotación
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).AlignCenter().Text("#");
                        h.Cell().Element(CeldaCabecera).Text("Producto");
                        h.Cell().Element(CeldaCabecera).Text("Categoría");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Uds.");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Ingresos");
                        if (mostrarRotacion)
                        {
                            h.Cell().Element(CeldaCabecera).AlignRight().Text("Stk. prom.");
                            h.Cell().Element(CeldaCabecera).AlignRight().Text("Rotación");
                        }
                    });

                    bool alt = false;
                    int i = 1;
                    foreach (var p in items)
                    {
                        var bg = alt ? ColorFilaAlt : ColorBlanco;
                        alt = !alt;

                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignCenter().Text(i.ToString()).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(p.Nombre).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(p.Categoria).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text(p.UnidadesVendidas.ToString()).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text($"Bs {p.Ingresos:N2}").FontColor(ColorCafe);
                        if (mostrarRotacion)
                        {
                            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                                .AlignRight().Text(p.StockPromedio.ToString("0.#")).FontColor(ColorCafe);
                            table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                                .AlignRight().Text(p.Rotacion.ToString("0.##")).FontColor(ColorCafe);
                        }
                        i++;
                    }

                    if (items.Count == 0)
                    {
                        uint span = (uint)(mostrarRotacion ? 7 : 5);
                        table.Cell().ColumnSpan(span).PaddingVertical(8).AlignCenter()
                            .Text("Sin datos para el período.")
                            .FontColor(Colors.Grey.Medium);
                    }
                });
            });
        }

        private void ComposePorCategoria(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Ventas por categoría").Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(4);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.ConstantColumn(80);
                        c.ConstantColumn(100);
                        c.ConstantColumn(60);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Categoría");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Uds.");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Ingresos");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Prods.");
                    });

                    bool alt = false;
                    foreach (var c in datos.PorCategoria)
                    {
                        var bg = alt ? ColorFilaAlt : ColorBlanco;
                        alt = !alt;

                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(c.Categoria).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text(c.UnidadesVendidas.ToString()).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text($"Bs {c.Ingresos:N2}").FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text(c.ProductosDistintos.ToString()).FontColor(ColorCafe);
                    }

                    if (datos.PorCategoria.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).PaddingVertical(8).AlignCenter()
                            .Text("Sin datos para el período.")
                            .FontColor(Colors.Grey.Medium);
                    }
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

        private static IContainer CeldaCabecera(IContainer container)
            => container
                .Background(ColorCabecer)
                .PaddingHorizontal(6)
                .PaddingVertical(5);

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }
}
