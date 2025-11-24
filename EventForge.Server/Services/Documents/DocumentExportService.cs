using EventForge.DTOs.Documents;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Implementation of document export service.
/// Supports multiple export formats: PDF, Excel, HTML, CSV, JSON.
/// 
/// PDF export uses QuestPDF (MIT License) for professional document generation.
/// Excel export uses ClosedXML (MIT License) for spreadsheet generation.
/// </summary>
public class DocumentExportService : IDocumentExportService
{
    private readonly EventForgeDbContext _context;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IDocumentAccessLogService _accessLogService;
    private readonly ILogger<DocumentExportService> _logger;
    private readonly ITenantContext _tenantContext;

    private readonly Dictionary<Guid, DocumentExportResultDto> _exportCache = new();

    public DocumentExportService(
        EventForgeDbContext context,
        IDocumentHeaderService documentHeaderService,
        IDocumentAccessLogService accessLogService,
        ILogger<DocumentExportService> logger,
        ITenantContext tenantContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _accessLogService = accessLogService ?? throw new ArgumentNullException(nameof(accessLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));

        // Set QuestPDF license to Community (for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<DocumentExportResultDto> ExportDocumentsAsync(
        DocumentExportRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var exportId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting document export {ExportId} for user {User} with format {Format}",
                exportId, currentUser, request.Format);

            // Build query for documents to export
            var query = _context.DocumentHeaders
                .Include(d => d.DocumentType)
                .Include(d => d.BusinessParty)
                .Include(d => d.Rows)
                .AsQueryable();

            // Apply filters
            if (request.TenantId.HasValue)
            {
                query = query.Where(d => d.TenantId == request.TenantId.Value);
            }
            else if (_tenantContext.CurrentTenantId.HasValue)
            {
                query = query.Where(d => d.TenantId == _tenantContext.CurrentTenantId.Value);
            }

            if (request.DocumentTypeId.HasValue)
            {
                query = query.Where(d => d.DocumentTypeId == request.DocumentTypeId.Value);
            }

            if (request.DocumentIds != null && request.DocumentIds.Any())
            {
                query = query.Where(d => request.DocumentIds.Contains(d.Id));
            }

            query = query.Where(d => d.Date >= request.FromDate && d.Date <= request.ToDate);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(d =>
                    d.Number.Contains(request.SearchTerm) ||
                    d.CustomerName!.Contains(request.SearchTerm) ||
                    d.Notes!.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<Data.Entities.Documents.DocumentStatus>(request.Status, true, out var status))
                {
                    query = query.Where(d => d.Status == status);
                }
            }

            if (request.MaxRecords.HasValue)
            {
                query = query.Take(request.MaxRecords.Value);
            }

            // Execute query
            var documents = await query.ToListAsync(cancellationToken);
            var documentDtos = documents.Select(MapToDto).ToList();

            _logger.LogInformation(
                "Retrieved {Count} documents for export {ExportId}",
                documents.Count, exportId);

            // Perform export based on format
            byte[]? fileData = null;
            string? fileName = null;

            var format = request.Format.ToUpperInvariant();
            switch (format)
            {
                case "PDF":
                    fileData = await ExportToPdfAsync(documentDtos, request.TemplateId, cancellationToken);
                    fileName = $"documents_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                    break;

                case "EXCEL":
                case "XLSX":
                    fileData = await ExportToExcelAsync(documentDtos, request.IncludeRows, cancellationToken);
                    fileName = $"documents_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "HTML":
                    var html = await ExportToHtmlAsync(documentDtos, request.TemplateId, cancellationToken);
                    fileData = Encoding.UTF8.GetBytes(html);
                    fileName = $"documents_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
                    break;

                case "CSV":
                    var csv = await ExportToCsvAsync(documentDtos, request.IncludeRows, cancellationToken);
                    fileData = Encoding.UTF8.GetBytes(csv);
                    fileName = $"documents_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                    break;

                case "JSON":
                    var json = await ExportToJsonAsync(documentDtos, cancellationToken);
                    fileData = Encoding.UTF8.GetBytes(json);
                    fileName = $"documents_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    break;

                default:
                    throw new ArgumentException($"Unsupported export format: {request.Format}");
            }

            var result = new DocumentExportResultDto
            {
                ExportId = exportId,
                Status = "Completed",
                Format = request.Format,
                DocumentCount = documents.Count,
                FileName = fileName,
                FileSizeBytes = fileData?.Length ?? 0,
                CreatedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                DownloadUrl = $"/api/v1/documents/exports/{exportId}/download",
                Metadata = new Dictionary<string, object>
                {
                    ["FromDate"] = request.FromDate,
                    ["ToDate"] = request.ToDate,
                    ["ExportedBy"] = currentUser
                }
            };

            // Cache the result for retrieval
            _exportCache[exportId] = result;

            // Log export access
            foreach (var doc in documents)
            {
                _ = await _accessLogService.LogAccessAsync(
                    doc.Id,
                    currentUser,
                    currentUser,
                    Data.Entities.Documents.DocumentAccessType.Export,
                    null,
                    null,
                    Data.Entities.Documents.AccessResult.Success,
                    $"Exported as {format}",
                    doc.TenantId,
                    null,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Completed export {ExportId} with {Count} documents in {Duration}ms",
                exportId, documents.Count, (DateTime.UtcNow - startTime).TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting documents for export {ExportId}", exportId);

            var errorResult = new DocumentExportResultDto
            {
                ExportId = exportId,
                Status = "Failed",
                Format = request.Format,
                CreatedAt = startTime,
                ErrorMessage = ex.Message
            };

            _exportCache[exportId] = errorResult;
            return errorResult;
        }
    }

    public Task<DocumentExportResultDto?> GetExportStatusAsync(
        Guid exportId,
        CancellationToken cancellationToken = default)
    {
        _ = _exportCache.TryGetValue(exportId, out var result);
        return Task.FromResult(result);
    }

    public Task<byte[]> ExportToPdfAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating PDF export for {Count} documents using QuestPDF",
            documents.Count());

        try
        {
            var documentsList = documents.ToList();

            // Generate PDF using QuestPDF
            var pdfBytes = Document.Create(container =>
            {
                _ = container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    _ = page.Header()
                        .AlignCenter()
                        .Text("Document Export Report")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(5);

                            // Export metadata
                            column.Item().Row(row =>
                            {
                                _ = row.RelativeItem().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(9);
                                _ = row.RelativeItem().AlignRight().Text($"Total Documents: {documentsList.Count}").FontSize(9).Bold();
                            });

                            _ = column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            // Documents table
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80);  // Number
                                    columns.ConstantColumn(80);  // Date
                                    columns.RelativeColumn(2);   // Customer
                                    columns.ConstantColumn(80);  // Total
                                    columns.ConstantColumn(70);  // Status
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    _ = header.Cell().Element(CellStyle).Text("Number").Bold();
                                    _ = header.Cell().Element(CellStyle).Text("Date").Bold();
                                    _ = header.Cell().Element(CellStyle).Text("Customer").Bold();
                                    _ = header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();
                                    _ = header.Cell().Element(CellStyle).Text("Status").Bold();

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Background(Colors.Grey.Lighten3)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten1)
                                            .Padding(5);
                                    }
                                });

                                // Data rows
                                foreach (var doc in documentsList)
                                {
                                    _ = table.Cell().Element(DataCellStyle).Text($"{doc.Series}{doc.Number}");
                                    _ = table.Cell().Element(DataCellStyle).Text(doc.Date.ToString("yyyy-MM-dd"));
                                    _ = table.Cell().Element(DataCellStyle).Text(doc.CustomerName ?? "N/A");
                                    _ = table.Cell().Element(DataCellStyle).AlignRight().Text($"{doc.TotalGrossAmount:C}");
                                    _ = table.Cell().Element(DataCellStyle).Text(doc.Status.ToString());

                                    static IContainer DataCellStyle(IContainer container)
                                    {
                                        return container
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5);
                                    }
                                }
                            });
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9));
                            _ = text.Span("Page ");
                            _ = text.CurrentPageNumber();
                            _ = text.Span(" of ");
                            _ = text.TotalPages();
                        });
                });
            }).GeneratePdf();

            _logger.LogInformation(
                "PDF export completed successfully. Size: {Size} bytes",
                pdfBytes.Length);

            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF export");
            throw new InvalidOperationException("Failed to generate PDF export", ex);
        }
    }

    public Task<byte[]> ExportToExcelAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating Excel export for {Count} documents using ClosedXML",
            documents.Count());

        try
        {
            var documentsList = documents.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Documents Export");

                // Add title
                worksheet.Range("A1:H1").Merge();
                worksheet.Cell("A1").Value = "Document Export Report";
                worksheet.Cell("A1").Style.Font.FontSize = 16;
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add metadata
                worksheet.Cell("A2").Value = "Generated:";
                worksheet.Cell("B2").Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell("A3").Value = "Total Documents:";
                worksheet.Cell("B3").Value = documentsList.Count;

                // Add header row
                int headerRow = 5;
                worksheet.Cell(headerRow, 1).Value = "Number";
                worksheet.Cell(headerRow, 2).Value = "Date";
                worksheet.Cell(headerRow, 3).Value = "Customer";
                worksheet.Cell(headerRow, 4).Value = "Net Total";
                worksheet.Cell(headerRow, 5).Value = "VAT";
                worksheet.Cell(headerRow, 6).Value = "Gross Total";
                worksheet.Cell(headerRow, 7).Value = "Currency";
                worksheet.Cell(headerRow, 8).Value = "Status";
                worksheet.Cell(headerRow, 9).Value = "Payment Status";

                // Style header
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Add data rows
                int currentRow = headerRow + 1;
                foreach (var doc in documentsList)
                {
                    worksheet.Cell(currentRow, 1).Value = $"{doc.Series}{doc.Number}";
                    worksheet.Cell(currentRow, 2).Value = doc.Date.ToString("yyyy-MM-dd");
                    worksheet.Cell(currentRow, 3).Value = doc.CustomerName ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = doc.TotalNetAmount;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 5).Value = doc.VatAmount;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 6).Value = doc.TotalGrossAmount;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 7).Value = doc.Currency ?? "EUR";
                    worksheet.Cell(currentRow, 8).Value = doc.Status.ToString();
                    worksheet.Cell(currentRow, 9).Value = doc.PaymentStatus.ToString();

                    // Add borders
                    var rowRange = worksheet.Range(currentRow, 1, currentRow, 9);
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.OutsideBorderColor = XLColor.Gray;

                    currentRow++;
                }

                // Add totals row
                if (documentsList.Any())
                {
                    currentRow++; // Skip a row
                    worksheet.Cell(currentRow, 3).Value = "TOTALS:";
                    worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

                    // Note: currentRow-2 gives the last data row (currentRow was incremented after loop, then incremented again above)
                    var lastDataRow = currentRow - 2;
                    worksheet.Cell(currentRow, 4).FormulaA1 = $"SUM(D{headerRow + 1}:D{lastDataRow})";
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 4).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 5).FormulaA1 = $"SUM(E{headerRow + 1}:E{lastDataRow})";
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 5).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 6).FormulaA1 = $"SUM(F{headerRow + 1}:F{lastDataRow})";
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 6).Style.Font.Bold = true;

                    // Style totals row
                    var totalsRange = worksheet.Range(currentRow, 3, currentRow, 6);
                    totalsRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 230, 241);
                    totalsRange.Style.Border.TopBorder = XLBorderStyleValues.Double;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Set column widths (override auto-fit for some columns)
                worksheet.Column(1).Width = 15; // Number
                worksheet.Column(2).Width = 12; // Date
                worksheet.Column(3).Width = 30; // Customer

                // Freeze header row
                worksheet.SheetView.FreezeRows(headerRow);

                _logger.LogInformation(
                    "Excel export completed successfully for {Count} documents",
                    documentsList.Count);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return Task.FromResult(stream.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel export");
            throw new InvalidOperationException("Failed to generate Excel export", ex);
        }
    }

    public Task<string> ExportToHtmlAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var html = new StringBuilder();
        _ = html.AppendLine("<!DOCTYPE html>");
        _ = html.AppendLine("<html>");
        _ = html.AppendLine("<head>");
        _ = html.AppendLine("<meta charset='utf-8' />");
        _ = html.AppendLine("<title>Document Export</title>");
        _ = html.AppendLine("<style>");
        _ = html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        _ = html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        _ = html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        _ = html.AppendLine("th { background-color: #4CAF50; color: white; }");
        _ = html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        _ = html.AppendLine("</style>");
        _ = html.AppendLine("</head>");
        _ = html.AppendLine("<body>");
        _ = html.AppendLine("<h1>Document Export</h1>");
        _ = html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        _ = html.AppendLine("<table>");
        _ = html.AppendLine("<tr>");
        _ = html.AppendLine("<th>Number</th>");
        _ = html.AppendLine("<th>Date</th>");
        _ = html.AppendLine("<th>Customer</th>");
        _ = html.AppendLine("<th>Total</th>");
        _ = html.AppendLine("<th>Status</th>");
        _ = html.AppendLine("</tr>");

        foreach (var doc in documents)
        {
            _ = html.AppendLine("<tr>");
            _ = html.AppendLine($"<td>{doc.Series}{doc.Number}</td>");
            _ = html.AppendLine($"<td>{doc.Date:yyyy-MM-dd}</td>");
            _ = html.AppendLine($"<td>{doc.CustomerName ?? "N/A"}</td>");
            _ = html.AppendLine($"<td>{doc.TotalGrossAmount:C}</td>");
            _ = html.AppendLine($"<td>{doc.Status}</td>");
            _ = html.AppendLine("</tr>");
        }

        _ = html.AppendLine("</table>");
        _ = html.AppendLine("</body>");
        _ = html.AppendLine("</html>");

        return Task.FromResult(html.ToString());
    }

    public Task<string> ExportToCsvAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default)
    {
        var csv = new StringBuilder();

        // Header
        _ = csv.AppendLine("\"Number\",\"Date\",\"Customer\",\"Net Total\",\"VAT\",\"Gross Total\",\"Currency\",\"Status\",\"Payment Status\"");

        // Data rows
        foreach (var doc in documents)
        {
            _ = csv.AppendLine($"\"{doc.Series}{doc.Number}\",\"{doc.Date:yyyy-MM-dd}\",\"{doc.CustomerName ?? ""}\",{doc.TotalNetAmount},{doc.VatAmount},{doc.TotalGrossAmount},\"{doc.Currency}\",\"{doc.Status}\",\"{doc.PaymentStatus}\"");
        }

        return Task.FromResult(csv.ToString());
    }

    public Task<string> ExportToJsonAsync(
        IEnumerable<DocumentHeaderDto> documents,
        CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(documents, options);
        return Task.FromResult(json);
    }

    private DocumentHeaderDto MapToDto(Data.Entities.Documents.DocumentHeader doc)
    {
        // Map entity DocumentStatus to DTO DocumentStatus
        var dtoStatus = doc.Status switch
        {
            Data.Entities.Documents.DocumentStatus.Open => DTOs.Common.DocumentStatus.Draft,
            Data.Entities.Documents.DocumentStatus.Closed => DTOs.Common.DocumentStatus.Approved,
            Data.Entities.Documents.DocumentStatus.Cancelled => DTOs.Common.DocumentStatus.Cancelled,
            _ => DTOs.Common.DocumentStatus.Draft
        };

        // Map entity PaymentStatus to DTO PaymentStatus
        var dtoPaymentStatus = doc.PaymentStatus switch
        {
            Data.Entities.Documents.PaymentStatus.Unpaid => DTOs.Common.PaymentStatus.Pending,
            Data.Entities.Documents.PaymentStatus.Paid => DTOs.Common.PaymentStatus.Paid,
            Data.Entities.Documents.PaymentStatus.PartiallyPaid => DTOs.Common.PaymentStatus.Partial,
            Data.Entities.Documents.PaymentStatus.Overdue => DTOs.Common.PaymentStatus.Pending, // Map Overdue to Pending
            _ => DTOs.Common.PaymentStatus.Pending
        };

        return new DocumentHeaderDto
        {
            Id = doc.Id,
            DocumentTypeId = doc.DocumentTypeId,
            Series = doc.Series,
            Number = doc.Number,
            Date = doc.Date,
            BusinessPartyId = doc.BusinessPartyId,
            CustomerName = doc.CustomerName,
            TotalNetAmount = doc.TotalNetAmount,
            VatAmount = doc.VatAmount,
            TotalGrossAmount = doc.TotalGrossAmount,
            Currency = doc.Currency,
            Status = dtoStatus,
            PaymentStatus = dtoPaymentStatus,
            Notes = doc.Notes,
            CreatedAt = doc.CreatedAt,
            CreatedBy = doc.CreatedBy
        };
    }
}
