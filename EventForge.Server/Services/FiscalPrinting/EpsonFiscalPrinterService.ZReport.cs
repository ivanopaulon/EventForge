using Prym.DTOs.FiscalPrinting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class EpsonFiscalPrinterService
{
    // -------------------------------------------------------------------------
    //  IFiscalPrinterService – PDF Z-report generation
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>
    /// The PDF is generated on-demand using <see cref="ZReportDocument"/> (QuestPDF)
    /// and cached in <c>DailyClosureRecord.PdfBytes</c> for subsequent downloads.
    /// The implementation is identical to the one used by
    /// <see cref="CustomFiscalPrinterService"/>.
    /// </remarks>
    public async Task<byte[]?> GenerateZReportPdfAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await context.DailyClosureRecords
                .Where(r => r.Id == closureId && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (record is null)
                return null;

            // Return cached PDF if already generated
            if (record.HasPdf && record.PdfBytes is { Length: > 0 })
                return record.PdfBytes;

            var printerName = await context.Printers
                .AsNoTracking()
                .Where(p => p.Id == record.PrinterId && !p.IsDeleted)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? record.PrinterId.ToString();

            var closureDto = new DailyClosureHistoryDto
            {
                Id = record.Id,
                PrinterId = record.PrinterId,
                PrinterName = printerName,
                ZReportNumber = record.ZReportNumber,
                ClosedAt = record.ClosedAt,
                ReceiptCount = record.ReceiptCount,
                TotalAmount = record.TotalAmount,
                CashAmount = record.CashAmount,
                CardAmount = record.CardAmount,
                Operator = record.Operator,
                HasPdf = true
            };

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var doc = new ZReportDocument(closureDto, printerName);
            var pdfBytes = doc.GeneratePdf();

            record.PdfBytes = pdfBytes;
            record.HasPdf = true;
            record.ModifiedAt = DateTime.UtcNow;
            record.ModifiedBy = "System";
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Epson Z-report PDF generated and cached | ClosureId={ClosureId} Size={Size}",
                closureId, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
