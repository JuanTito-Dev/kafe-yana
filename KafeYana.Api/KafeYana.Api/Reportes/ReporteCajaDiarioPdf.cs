using KafeYana.Application.Dtos.ReporteDtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KafeYana.Api.Reportes
{
    /// <summary>
    /// PDF del reporte diario de cajas: resumen del día + tabla de sesiones
    /// (con badge ABIERTA/CERRADA) + consolidado de movimientos.
    /// </summary>
    public class ReporteCajaDiarioPdf(DtoReporteDiarioCaja datos) : IDocument
    {
        private static readonly string ColorCafe     = "#7B3F00";
        private static readonly string ColorCabecer  = "#8B4513";
        private static readonly string ColorFilaAlt  = "#F5E6D3";
        private static readonly string ColorBlanco   = "#FFFFFF";
        private static readonly string ColorAbierta  = "#DC2626";
        private static readonly string ColorCerrada  = "#16A34A";

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

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text($"Reporte Diario de Caja — {datos.Fecha:dd/MM/yyyy}")
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
                col.Item().Element(ComposeSesiones);

                if (datos.Movimientos.Count > 0)
                    col.Item().Element(ComposeMovimientos);
            });
        }

        // ── Resumen ────────────────────────────────────────────────────────────
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

                    FilaResumen(table, "Cajas iniciadas", datos.Resumen.CajasIniciadas.ToString(), true);
                    FilaResumen(table, "Cajas cerradas", datos.Resumen.CajasCerradas.ToString(), false);
                    FilaResumen(table, "Caja abierta actualmente", datos.Resumen.HayCajaAbierta ? "Sí" : "No", true);
                    FilaResumen(table, "Saldo inicial total", $"Bs {datos.Resumen.SaldoInicialTotal:N2}", false);
                    FilaResumen(table, "Total ventas POS", $"Bs {datos.Resumen.TotalVentas:N2}", true);
                    FilaResumen(table, "  Efectivo", $"Bs {datos.Resumen.TotalEfectivo:N2}", false);
                    FilaResumen(table, "  Tarjeta", $"Bs {datos.Resumen.TotalTarjeta:N2}", true);
                    FilaResumen(table, "  QR", $"Bs {datos.Resumen.TotalQr:N2}", false);
                    FilaResumen(table, "Total ingresos", $"Bs {datos.Resumen.TotalIngresos:N2}", true);
                    FilaResumen(table, "Total egresos", $"Bs {datos.Resumen.TotalEgresos:N2}", false);
                    FilaResumen(table, "Balance neto", $"Bs {datos.Resumen.BalanceNeto:N2}", true);
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

        // ── Sesiones del día ───────────────────────────────────────────────────
        private void ComposeSesiones(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"Sesiones del día ({datos.Cajas.Count})")
                    .Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(4);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(65);  // Código
                        c.RelativeColumn(2);   // Apertura
                        c.RelativeColumn(2);   // Cierre
                        c.RelativeColumn(1);   // Saldo inicial
                        c.RelativeColumn(1);   // Ventas
                        c.RelativeColumn(1);   // Ingresos
                        c.RelativeColumn(1);   // Egresos
                        c.ConstantColumn(55);  // Estado
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Código");
                        h.Cell().Element(CeldaCabecera).Text("Apertura");
                        h.Cell().Element(CeldaCabecera).Text("Cierre");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("S. Inicial");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Ventas");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Ingresos");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Egresos");
                        h.Cell().Element(CeldaCabecera).AlignCenter().Text("Estado");
                    });

                    bool alt = false;
                    foreach (var c in datos.Cajas)
                    {
                        var bg = alt ? ColorFilaAlt : ColorBlanco;
                        alt = !alt;

                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .Text(c.Codigo).FontColor(ColorCafe);

                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .Text(c.Apertura.ToLocalTime().ToString("dd/MM HH:mm")).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .Text(c.Cierre?.ToLocalTime().ToString("dd/MM HH:mm") ?? "—").FontColor(ColorCafe);

                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .AlignRight().Text($"Bs {c.SaldoInicial:N2}").FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .AlignRight().Text($"Bs {c.TotalVentas:N2}").FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .AlignRight().Text($"Bs {c.TotalIngresos:N2}").FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .AlignRight().Text($"Bs {c.TotalEgresos:N2}").FontColor(ColorCafe);

                        var estadoColor = c.AbiertaActualmente ? ColorAbierta : ColorCerrada;
                        table.Cell().Background(bg).PaddingHorizontal(5).PaddingVertical(4)
                            .AlignCenter().Text(c.Estado)
                            .FontColor(estadoColor).Bold();
                    }

                    if (datos.Cajas.Count == 0)
                    {
                        table.Cell().ColumnSpan(8).PaddingVertical(8).AlignCenter()
                            .Text("No hay sesiones iniciadas en esta fecha.")
                            .FontColor(Colors.Grey.Medium);
                    }
                });
            });
        }

        // ── Movimientos ────────────────────────────────────────────────────────
        private void ComposeMovimientos(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"Movimientos del día ({datos.Movimientos.Count})")
                    .Bold().FontSize(11).FontColor(ColorCafe);
                col.Item().Height(4);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(60);     // Caja
                        c.ConstantColumn(70);     // Hora
                        c.ConstantColumn(50);     // Tipo
                        c.RelativeColumn(2);      // Categoría
                        c.RelativeColumn(4);      // Descripción
                        c.RelativeColumn(1);      // Monto
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaCabecera).Text("Caja");
                        h.Cell().Element(CeldaCabecera).Text("Hora");
                        h.Cell().Element(CeldaCabecera).Text("Tipo");
                        h.Cell().Element(CeldaCabecera).Text("Categoría");
                        h.Cell().Element(CeldaCabecera).Text("Descripción");
                        h.Cell().Element(CeldaCabecera).AlignRight().Text("Monto");
                    });

                    bool alt = false;
                    foreach (var m in datos.Movimientos)
                    {
                        var bg = alt ? ColorFilaAlt : ColorBlanco;
                        alt = !alt;
                        var esIngreso = m.Monto >= 0;

                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(m.CodigoCaja).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(m.Fecha.ToLocalTime().ToString("HH:mm")).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(m.Tipo).FontColor(esIngreso ? ColorCerrada : ColorAbierta);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(m.Categoria).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .Text(m.Descripcion).FontColor(ColorCafe);
                        table.Cell().Background(bg).PaddingHorizontal(4).PaddingVertical(3)
                            .AlignRight().Text($"Bs {Math.Abs(m.Monto):N2}")
                            .FontColor(esIngreso ? ColorCerrada : ColorAbierta);
                    }
                });
            });
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
    }
}
