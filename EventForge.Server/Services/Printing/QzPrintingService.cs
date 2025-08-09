using EventForge.DTOs.Printing;
using EventForge.Server.Services.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Printing;

/// <summary>
/// Implementation of QZ Tray printing service using WebSocket communication with digital signature support
/// </summary>
public class QzPrintingService : IQzPrintingService
{
    private readonly ILogger<QzPrintingService> _logger;
    private readonly QzDigitalSignatureService _signatureService;
    private readonly Dictionary<Guid, PrintJobDto> _printJobs;
    private readonly SemaphoreSlim _semaphore;

    public QzPrintingService(ILogger<QzPrintingService> logger, QzDigitalSignatureService signatureService)
    {
        _logger = logger;
        _signatureService = signatureService;
        _printJobs = new Dictionary<Guid, PrintJobDto>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<PrinterDiscoveryResponseDto> DiscoverPrintersAsync(PrinterDiscoveryRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting printer discovery with QZ URL: {QzUrl}", request.QzUrl ?? "ws://localhost:8182");

            var qzUrl = request.QzUrl ?? "ws://localhost:8182";
            using var client = new ClientWebSocket();

            var uri = new Uri(qzUrl);
            await client.ConnectAsync(uri, cancellationToken);

            // Send discovery command to QZ Tray
            var discoveryCommand = new
            {
                call = "qz.printers.find",
                @params = new object[] { }
            };

            var commandJson = JsonSerializer.Serialize(discoveryCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            // Read response
            var response = await ReceiveWebSocketMessage(client, cancellationToken);
            var printers = ParsePrintersFromResponse(response);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Discovery complete", cancellationToken);

            return new PrinterDiscoveryResponseDto
            {
                Success = true,
                Printers = printers,
                DiscoveredAt = DateTime.UtcNow,
                ConnectionStatus = QzConnectionStatus.Connected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering printers from QZ Tray");
            return new PrinterDiscoveryResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ConnectionStatus = QzConnectionStatus.Error
            };
        }
    }

    /// <inheritdoc />
    public async Task<PrinterStatusResponseDto> CheckPrinterStatusAsync(PrinterStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking status for printer: {PrinterId}", request.PrinterId);

            using var client = new ClientWebSocket();
            var uri = new Uri("ws://localhost:8182");
            await client.ConnectAsync(uri, cancellationToken);

            // Send status check command
            var statusCommand = new
            {
                call = "qz.printers.getStatus",
                @params = new object[] { request.PrinterId }
            };

            var commandJson = JsonSerializer.Serialize(statusCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessage(client, cancellationToken);
            var status = ParseStatusFromResponse(response);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Status check complete", cancellationToken);

            return new PrinterStatusResponseDto
            {
                Success = true,
                Status = status,
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking printer status");
            return new PrinterStatusResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                Status = EventForge.DTOs.Printing.PrinterStatus.Error
            };
        }
    }

    /// <inheritdoc />
    public async Task<SubmitPrintJobResponseDto> SubmitPrintJobAsync(SubmitPrintJobRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting print job: {JobTitle} to printer: {PrinterId}",
                request.PrintJob.Title, request.PrintJob.PrinterId);

            // Store print job
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                request.PrintJob.Status = PrintJobStatus.Queued;
                request.PrintJob.SubmittedAt = DateTime.UtcNow;
                _printJobs[request.PrintJob.Id] = request.PrintJob;
            }
            finally
            {
                _semaphore.Release();
            }

            using var client = new ClientWebSocket();
            var uri = new Uri("ws://localhost:8182");
            await client.ConnectAsync(uri, cancellationToken);

            // Create print command based on content type
            var printCommand = CreatePrintCommand(request.PrintJob);

            // Sign the payload with digital signature
            var signedCommand = await _signatureService.SignPayloadAsync(printCommand);

            var commandJson = JsonSerializer.Serialize(signedCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);

            _logger.LogDebug("Sending signed payload to QZ Tray: {PayloadLength} bytes", commandBytes.Length);

            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessage(client, cancellationToken);
            var qzJobId = ParseJobIdFromResponse(response);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Print job submitted", cancellationToken);

            // Update job status
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_printJobs.ContainsKey(request.PrintJob.Id))
                {
                    _printJobs[request.PrintJob.Id].Status = PrintJobStatus.Printing;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return new SubmitPrintJobResponseDto
            {
                Success = true,
                PrintJob = request.PrintJob,
                QzJobId = qzJobId,
                EstimatedCompletion = DateTime.UtcNow.AddMinutes(2) // Estimate 2 minutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting print job");

            // Update job status to failed
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_printJobs.ContainsKey(request.PrintJob.Id))
                {
                    _printJobs[request.PrintJob.Id].Status = PrintJobStatus.Failed;
                    _printJobs[request.PrintJob.Id].ErrorMessage = ex.Message;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return new SubmitPrintJobResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                PrintJob = request.PrintJob
            };
        }
    }

    /// <inheritdoc />
    public async Task<PrintJobDto?> GetPrintJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _printJobs.TryGetValue(jobId, out var job) ? job : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelPrintJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_printJobs.TryGetValue(jobId, out var job))
            {
                job.Status = PrintJobStatus.Cancelled;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Print job {JobId} cancelled", jobId);
                return true;
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestQzConnectionAsync(string qzUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ClientWebSocket();
            var uri = new Uri(qzUrl);
            await client.ConnectAsync(uri, cancellationToken);
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection test", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QZ connection test failed for URL: {QzUrl}", qzUrl);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetQzVersionAsync(string qzUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ClientWebSocket();
            var uri = new Uri(qzUrl);
            await client.ConnectAsync(uri, cancellationToken);

            var versionCommand = new
            {
                call = "qz.websocket.getVersion",
                @params = new object[] { }
            };

            var commandJson = JsonSerializer.Serialize(versionCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessage(client, cancellationToken);
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Version check complete", cancellationToken);

            return ParseVersionFromResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QZ version");
            return null;
        }
    }

    /// <summary>
    /// Validates that the QZ Tray digital signature configuration is properly set up
    /// </summary>
    /// <returns>True if signature configuration is valid</returns>
    public async Task<bool> ValidateSignatureConfigurationAsync()
    {
        try
        {
            return await _signatureService.ValidateSigningConfigurationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QZ Tray signature configuration");
            return false;
        }
    }

    private async Task<string> ReceiveWebSocketMessage(ClientWebSocket client, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private List<PrinterDto> ParsePrintersFromResponse(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var printers = new List<PrinterDto>();

            if (document.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
            {
                foreach (var printerElement in result.EnumerateArray())
                {
                    if (printerElement.ValueKind == JsonValueKind.String)
                    {
                        var printerName = printerElement.GetString();
                        if (!string.IsNullOrEmpty(printerName))
                        {
                            printers.Add(new PrinterDto
                            {
                                Id = printerName,
                                Name = printerName,
                                Status = EventForge.DTOs.Printing.PrinterStatus.Online,
                                IsAvailable = true,
                                LastStatusUpdate = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing printers from QZ response: {Response}", response);
            return new List<PrinterDto>();
        }
    }

    private EventForge.DTOs.Printing.PrinterStatus ParseStatusFromResponse(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            if (document.RootElement.TryGetProperty("result", out var result))
            {
                // QZ Tray returns printer status information
                // This is a simplified implementation - real QZ status checking would be more complex
                return EventForge.DTOs.Printing.PrinterStatus.Online;
            }

            return EventForge.DTOs.Printing.PrinterStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing status from QZ response: {Response}", response);
            return EventForge.DTOs.Printing.PrinterStatus.Error;
        }
    }

    private string? ParseJobIdFromResponse(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            if (document.RootElement.TryGetProperty("jobId", out var jobId))
            {
                return jobId.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing job ID from QZ response: {Response}", response);
            return null;
        }
    }

    private string? ParseVersionFromResponse(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            if (document.RootElement.TryGetProperty("result", out var result))
            {
                return result.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing version from QZ response: {Response}", response);
            return null;
        }
    }

    private object CreatePrintCommand(PrintJobDto printJob)
    {
        return printJob.ContentType switch
        {
            PrintContentType.Raw => new
            {
                call = "qz.print",
                @params = new object[]
                {
                    new
                    {
                        printer = printJob.PrinterId,
                        data = new[]
                        {
                            new
                            {
                                type = "raw",
                                data = printJob.Content
                            }
                        }
                    }
                }
            },
            PrintContentType.Html => new
            {
                call = "qz.print",
                @params = new object[]
                {
                    new
                    {
                        printer = printJob.PrinterId,
                        data = new[]
                        {
                            new
                            {
                                type = "html",
                                data = printJob.Content
                            }
                        }
                    }
                }
            },
            _ => new
            {
                call = "qz.print",
                @params = new object[]
                {
                    new
                    {
                        printer = printJob.PrinterId,
                        data = new[]
                        {
                            new
                            {
                                type = "raw",
                                data = printJob.Content
                            }
                        }
                    }
                }
            }
        };
    }
}