using Prym.DTOs.FiscalPrinting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// QuestPDF document that renders a Z-Report (daily fiscal closure summary) as a PDF.
/// </summary>
internal sealed class ZReportDocument : IDocument
{
    private readonly DailyClosureHistoryDto _closure;
    private readonly string _printerName;

    public ZReportDocument(DailyClosureHistoryDto closure, string printerName)
    {
        _closure = closure;
        _printerName = printerName;
    }

    // ── IDocument ─────────────────────────────────────────────────────────────

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Z-Report #{_closure.ZReportNumber} – {_printerName}",
        Author = _closure.Operator ?? "Sistema",
        CreationDate = _closure.ClosedAt
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(20, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("Z-REPORT – CHIUSURA GIORNALIERA")
                .Bold().FontSize(14);

            col.Item().AlignCenter().Text(_printerName)
                .FontSize(11).FontColor(Colors.Grey.Darken2);

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    // ── Content ───────────────────────────────────────────────────────────────

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                });

                AddRow(table, "N° Z-Report:", _closure.ZReportNumber.ToString("D4"));
                AddRow(table, "Data/Ora chiusura:",
                    _closure.ClosedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"));

                if (!string.IsNullOrWhiteSpace(_closure.Operator))
                    AddRow(table, "Operatore:", _closure.Operator);

                AddSeparatorRow(table);

                AddRow(table, "N° scontrini:", _closure.ReceiptCount.ToString("N0"));
                AddRowBold(table, "TOTALE GIORNATA:", $"€ {_closure.TotalAmount:N2}");

                AddSeparatorRow(table);

                if (_closure.CashAmount > 0)
                    AddRow(table, "  di cui Contanti:", $"€ {_closure.CashAmount:N2}");

                if (_closure.CardAmount > 0)
                    AddRow(table, "  di cui Carte/Elettronico:", $"€ {_closure.CardAmount:N2}");
            });

            col.Item().PaddingTop(12).Text(
                "Documento generato automaticamente da EventForge. " +
                "Conservare il presente documento per 10 anni ai sensi " +
                "delle norme fiscali vigenti.")
                .FontSize(8).FontColor(Colors.Grey.Darken2).Italic();
        });
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(
                    $"Generato: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                    .FontSize(8).FontColor(Colors.Grey.Darken2);

                row.RelativeItem().AlignRight().Text(ctx =>
                {
                    ctx.Span("Pagina ").FontSize(8).FontColor(Colors.Grey.Darken2);
                    ctx.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                    ctx.Span(" di ").FontSize(8).FontColor(Colors.Grey.Darken2);
                    ctx.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().PaddingVertical(2).Text(label);
        table.Cell().PaddingVertical(2).AlignRight().Text(value ?? string.Empty);
    }

    private static void AddRowBold(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(2).Text(label).Bold();
        table.Cell().PaddingVertical(2).AlignRight().Text(value).Bold();
    }

    private static void AddSeparatorRow(TableDescriptor table)
    {
        table.Cell().ColumnSpan(2).PaddingVertical(4).LineHorizontal(0.5f)
            .LineColor(Colors.Grey.Medium);
    }
}
