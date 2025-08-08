using EventForge.DTOs.Printing;

namespace EventForge.Server.Services.Interfaces;

/// <summary>
/// Interface for QZ Tray print service operations.
/// Provides functionality to discover printers, check status, and submit print jobs through QZ Tray.
/// </summary>
public interface IQzPrintingService
{
    /// <summary>
    /// Discovers available printers through QZ Tray
    /// </summary>
    /// <param name="request">Discovery request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovery response with available printers</returns>
    Task<PrinterDiscoveryResponseDto> DiscoverPrintersAsync(PrinterDiscoveryRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a specific printer
    /// </summary>
    /// <param name="request">Status check request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Printer status response</returns>
    Task<PrinterStatusResponseDto> CheckPrinterStatusAsync(PrinterStatusRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a print job to QZ Tray
    /// </summary>
    /// <param name="request">Print job submission request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Print job submission response</returns>
    Task<SubmitPrintJobResponseDto> SubmitPrintJobAsync(SubmitPrintJobRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a print job
    /// </summary>
    /// <param name="jobId">Print job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current print job information</returns>
    Task<PrintJobDto?> GetPrintJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending or active print job
    /// </summary>
    /// <param name="jobId">Print job ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelPrintJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to a QZ Tray instance
    /// </summary>
    /// <param name="qzUrl">QZ Tray URL to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test result</returns>
    Task<bool> TestQzConnectionAsync(string qzUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about QZ Tray version and capabilities
    /// </summary>
    /// <param name="qzUrl">QZ Tray URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QZ information or null if connection failed</returns>
    Task<string?> GetQzVersionAsync(string qzUrl, CancellationToken cancellationToken = default);
}