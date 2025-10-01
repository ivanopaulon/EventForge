using EventForge.DTOs.Documents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for document export operations.
/// Supports multiple export formats: PDF, Excel, HTML, CSV, JSON.
/// </summary>
public interface IDocumentExportService
{
    /// <summary>
    /// Initiates a document export operation.
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <param name="currentUser">User initiating the export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation result with status and download info</returns>
    Task<DocumentExportResultDto> ExportDocumentsAsync(
        DocumentExportRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of an export operation.
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation status</returns>
    Task<DocumentExportResultDto?> GetExportStatusAsync(
        Guid exportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports documents to PDF format.
    /// </summary>
    /// <param name="documents">Documents to export</param>
    /// <param name="templateId">Optional template ID for custom formatting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportToPdfAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports documents to Excel format.
    /// </summary>
    /// <param name="documents">Documents to export</param>
    /// <param name="includeRows">Include document rows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportToExcelAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports documents to HTML format.
    /// </summary>
    /// <param name="documents">Documents to export</param>
    /// <param name="templateId">Optional template ID for custom formatting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTML content as string</returns>
    Task<string> ExportToHtmlAsync(
        IEnumerable<DocumentHeaderDto> documents,
        Guid? templateId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports documents to CSV format.
    /// </summary>
    /// <param name="documents">Documents to export</param>
    /// <param name="includeRows">Include document rows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV content as string</returns>
    Task<string> ExportToCsvAsync(
        IEnumerable<DocumentHeaderDto> documents,
        bool includeRows = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports documents to JSON format.
    /// </summary>
    /// <param name="documents">Documents to export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON content as string</returns>
    Task<string> ExportToJsonAsync(
        IEnumerable<DocumentHeaderDto> documents,
        CancellationToken cancellationToken = default);
}
