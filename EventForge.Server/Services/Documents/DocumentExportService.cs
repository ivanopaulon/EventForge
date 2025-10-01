using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Implementation of document export service.
/// Supports multiple export formats: PDF, Excel, HTML, CSV, JSON.
/// 
/// NOTE: PDF and Excel exports are currently stub implementations.
/// To enable full functionality, add the following packages:
/// - For PDF: iText7 or QuestPDF
/// - For Excel: EPPlus or ClosedXML
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
                await _accessLogService.LogAccessAsync(
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
        _exportCache.TryGetValue(exportId, out var result);
        return Task.FromResult(result);
    }

    public Task<byte[]> ExportToPdfAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "PDF export is a stub implementation. Install iText7 or QuestPDF for full functionality.");

        // Stub implementation - returns a simple text representation
        var content = new StringBuilder();
        content.AppendLine("DOCUMENT EXPORT - PDF");
        content.AppendLine("====================");
        content.AppendLine();

        foreach (var doc in documents)
        {
            content.AppendLine($"Document: {doc.Series}{doc.Number}");
            content.AppendLine($"Date: {doc.Date:yyyy-MM-dd}");
            content.AppendLine($"Customer: {doc.CustomerName ?? "N/A"}");
            content.AppendLine($"Total: {doc.TotalGrossAmount:C}");
            content.AppendLine($"Status: {doc.Status}");
            content.AppendLine("---");
            content.AppendLine();
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(content.ToString()));
    }

    public Task<byte[]> ExportToExcelAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Excel export is a stub implementation. Install EPPlus or ClosedXML for full functionality.");

        // Stub implementation - returns CSV format as byte array
        var csv = ExportToCsvAsync(documents, includeRows, cancellationToken).Result;
        return Task.FromResult(Encoding.UTF8.GetBytes(csv));
    }

    public Task<string> ExportToHtmlAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8' />");
        html.AppendLine("<title>Document Export</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #4CAF50; color: white; }");
        html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<h1>Document Export</h1>");
        html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        html.AppendLine("<table>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>Number</th>");
        html.AppendLine("<th>Date</th>");
        html.AppendLine("<th>Customer</th>");
        html.AppendLine("<th>Total</th>");
        html.AppendLine("<th>Status</th>");
        html.AppendLine("</tr>");

        foreach (var doc in documents)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{doc.Series}{doc.Number}</td>");
            html.AppendLine($"<td>{doc.Date:yyyy-MM-dd}</td>");
            html.AppendLine($"<td>{doc.CustomerName ?? "N/A"}</td>");
            html.AppendLine($"<td>{doc.TotalGrossAmount:C}</td>");
            html.AppendLine($"<td>{doc.Status}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return Task.FromResult(html.ToString());
    }

    public Task<string> ExportToCsvAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("\"Number\",\"Date\",\"Customer\",\"Net Total\",\"VAT\",\"Gross Total\",\"Currency\",\"Status\",\"Payment Status\"");

        // Data rows
        foreach (var doc in documents)
        {
            csv.AppendLine($"\"{doc.Series}{doc.Number}\",\"{doc.Date:yyyy-MM-dd}\",\"{doc.CustomerName ?? ""}\",{doc.TotalNetAmount},{doc.VatAmount},{doc.TotalGrossAmount},\"{doc.Currency}\",\"{doc.Status}\",\"{doc.PaymentStatus}\"");
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
