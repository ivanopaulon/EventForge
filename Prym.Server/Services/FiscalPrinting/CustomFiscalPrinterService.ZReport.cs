using Prym.DTOs.FiscalPrinting;
using Prym.Server.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace Prym.Server.Services.FiscalPrinting;

public partial class CustomFiscalPrinterService
{
    /// <inheritdoc />
    public async Task<byte[]?> GenerateZReportPdfAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        var record = await context.DailyClosureRecords
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return null;

        // Return cached PDF if already generated
        if (record.HasPdf && record.PdfBytes is { Length: > 0 })
            return record.PdfBytes;

        // Load printer name
        var printerName = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == record.PrinterId && !p.IsDeleted)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? record.PrinterId.ToString();

        // Build the DTO for the document
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

        // Generate PDF with QuestPDF
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var doc = new ZReportDocument(closureDto, printerName);
        var pdfBytes = doc.GeneratePdf();

        // Persist PDF bytes and mark HasPdf
        record.PdfBytes = pdfBytes;
        record.HasPdf = true;
        record.ModifiedAt = DateTime.UtcNow;
        record.ModifiedBy = "System";
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ZReport PDF generated | ClosureId={ClosureId} Bytes={Bytes}",
            closureId, pdfBytes.Length);

        return pdfBytes;
    }
}
