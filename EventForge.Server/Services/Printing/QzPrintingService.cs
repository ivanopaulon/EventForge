using EventForge.DTOs.Printing;
using EventForge.Server.Services.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Printing;

/// <summary>
/// Implementation of QZ Tray printing service using WebSocket communication
/// </summary>
public class QzPrintingService : IQzPrintingService
{
    private readonly ILogger<QzPrintingService> _logger;
    private readonly Dictionary<Guid, PrintJobDto> _printJobs;
    private readonly SemaphoreSlim _semaphore;

    public QzPrintingService(ILogger<QzPrintingService> logger)
    {
        _logger = logger;
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

            // Generate unique message ID for request/response matching
            var messageUid = Guid.NewGuid().ToString();

            // Send discovery command to QZ Tray with proper structure
            var discoveryCommand = new
            {
                call = "qz.printers.find",
                @params = new object[] { },
                uid = messageUid
            };

            var commandJson = JsonSerializer.Serialize(discoveryCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            
            _logger.LogDebug("Sending QZ command: {Command}", commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            // Wait for response with timeout
            var response = await ReceiveWebSocketMessageWithTimeout(client, request.TimeoutMs, cancellationToken);
            _logger.LogDebug("Received QZ response: {Response}", response);

            var (printers, qzVersion) = ParsePrintersFromResponse(response, messageUid);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Discovery complete", cancellationToken);

            return new PrinterDiscoveryResponseDto
            {
                Success = true,
                Printers = printers,
                DiscoveredAt = DateTime.UtcNow,
                ConnectionStatus = QzConnectionStatus.Connected,
                QzVersion = qzVersion
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

            var messageUid = Guid.NewGuid().ToString();
            
            // Send status check command with proper UID
            var statusCommand = new
            {
                call = "qz.printers.getStatus",
                @params = new object[] { request.PrinterId },
                uid = messageUid
            };

            var commandJson = JsonSerializer.Serialize(statusCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            
            _logger.LogDebug("Sending QZ status command: {Command}", commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessageWithTimeout(client, request.TimeoutMs, cancellationToken);
            var status = ParseStatusFromResponse(response, messageUid);

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
            _logger.LogError(ex, "Error checking printer status for {PrinterId}", request.PrinterId);
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

            var messageUid = Guid.NewGuid().ToString();
            
            // Send print command based on content type with proper UID
            var printCommand = CreatePrintCommand(request.PrintJob, messageUid);
            var commandJson = JsonSerializer.Serialize(printCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);

            _logger.LogDebug("Sending QZ print command: {Command}", commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessageWithTimeout(client, request.CompletionTimeoutMs, cancellationToken);
            var qzJobId = ParseJobIdFromResponse(response, messageUid);

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

            var messageUid = Guid.NewGuid().ToString();
            var versionCommand = new
            {
                call = "qz.websocket.getVersion",
                @params = new object[] { },
                uid = messageUid
            };

            var commandJson = JsonSerializer.Serialize(versionCommand);
            var commandBytes = Encoding.UTF8.GetBytes(commandJson);
            
            _logger.LogDebug("Sending QZ version command: {Command}", commandJson);
            await client.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, cancellationToken);

            var response = await ReceiveWebSocketMessageWithTimeout(client, 10000, cancellationToken);
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Version check complete", cancellationToken);

            return ParseVersionFromResponse(response, messageUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QZ version");
            return null;
        }
    }

    private async Task<string> ReceiveWebSocketMessage(ClientWebSocket client, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private async Task<string> ReceiveWebSocketMessageWithTimeout(ClientWebSocket client, int timeoutMs, CancellationToken cancellationToken)
    {
        using var timeoutCancellation = new CancellationTokenSource(timeoutMs);
        using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

        var buffer = new List<byte>();
        var tempBuffer = new byte[4096];
        
        try
        {
            WebSocketReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(new ArraySegment<byte>(tempBuffer), combinedCancellation.Token);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    buffer.AddRange(tempBuffer.Take(result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new InvalidOperationException("WebSocket connection closed by QZ Tray");
                }
            }
            while (!result.EndOfMessage);

            return Encoding.UTF8.GetString(buffer.ToArray());
        }
        catch (OperationCanceledException) when (timeoutCancellation.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"QZ Tray response timeout after {timeoutMs}ms");
        }
    }

    private (List<PrinterDto> printers, string? qzVersion) ParsePrintersFromResponse(string response, string expectedUid)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var printers = new List<PrinterDto>();
            string? qzVersion = null;

            _logger.LogDebug("Parsing QZ response: {Response}", response);

            // Check if this is an error response
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorMessage = errorElement.GetString() ?? "Unknown QZ Tray error";
                _logger.LogError("QZ Tray returned error: {Error}", errorMessage);
                throw new InvalidOperationException($"QZ Tray error: {errorMessage}");
            }

            // Verify the UID matches our request
            if (document.RootElement.TryGetProperty("uid", out var uidElement))
            {
                var responseUid = uidElement.GetString();
                if (responseUid != expectedUid)
                {
                    _logger.LogWarning("QZ response UID mismatch. Expected: {Expected}, Got: {Received}", expectedUid, responseUid);
                }
            }

            // Extract QZ version if available
            if (document.RootElement.TryGetProperty("qzVersion", out var versionElement))
            {
                qzVersion = versionElement.GetString();
            }

            // Parse printer list from result
            if (document.RootElement.TryGetProperty("result", out var result))
            {
                if (result.ValueKind == JsonValueKind.Array)
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
                                    LastStatusUpdate = DateTime.UtcNow,
                                    Description = $"Printer discovered via QZ Tray"
                                });
                            }
                        }
                        else if (printerElement.ValueKind == JsonValueKind.Object)
                        {
                            // Handle printer objects with more details
                            var printerName = printerElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                            var printerDescription = printerElement.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
                            
                            if (!string.IsNullOrEmpty(printerName))
                            {
                                printers.Add(new PrinterDto
                                {
                                    Id = printerName,
                                    Name = printerName,
                                    Status = EventForge.DTOs.Printing.PrinterStatus.Online,
                                    IsAvailable = true,
                                    LastStatusUpdate = DateTime.UtcNow,
                                    Description = printerDescription ?? $"Printer discovered via QZ Tray"
                                });
                            }
                        }
                    }
                }
                else if (result.ValueKind == JsonValueKind.String)
                {
                    // Single printer result
                    var printerName = result.GetString();
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        printers.Add(new PrinterDto
                        {
                            Id = printerName,
                            Name = printerName,
                            Status = EventForge.DTOs.Printing.PrinterStatus.Online,
                            IsAvailable = true,
                            LastStatusUpdate = DateTime.UtcNow,
                            Description = $"Printer discovered via QZ Tray"
                        });
                    }
                }
            }

            _logger.LogInformation("Successfully parsed {Count} printers from QZ Tray response", printers.Count);
            return (printers, qzVersion);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response from QZ Tray: {Response}", response);
            throw new InvalidOperationException($"Invalid JSON response from QZ Tray: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing printers from QZ response: {Response}", response);
            throw;
        }
    }

    private EventForge.DTOs.Printing.PrinterStatus ParseStatusFromResponse(string response, string expectedUid)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            // Check for errors
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorMessage = errorElement.GetString() ?? "Unknown QZ Tray error";
                _logger.LogError("QZ Tray status check returned error: {Error}", errorMessage);
                return EventForge.DTOs.Printing.PrinterStatus.Error;
            }

            // Verify UID matches
            if (document.RootElement.TryGetProperty("uid", out var uidElement))
            {
                var responseUid = uidElement.GetString();
                if (responseUid != expectedUid)
                {
                    _logger.LogWarning("QZ status response UID mismatch. Expected: {Expected}, Got: {Received}", expectedUid, responseUid);
                }
            }

            if (document.RootElement.TryGetProperty("result", out var result))
            {
                // QZ Tray returns printer status information
                // This could be a boolean (true/false for available) or an object with status details
                if (result.ValueKind == JsonValueKind.True)
                {
                    return EventForge.DTOs.Printing.PrinterStatus.Online;
                }
                else if (result.ValueKind == JsonValueKind.False)
                {
                    return EventForge.DTOs.Printing.PrinterStatus.Offline;
                }
                else if (result.ValueKind == JsonValueKind.Object)
                {
                    // Handle detailed status object
                    if (result.TryGetProperty("available", out var availableElement) && availableElement.ValueKind == JsonValueKind.True)
                    {
                        return EventForge.DTOs.Printing.PrinterStatus.Online;
                    }
                    return EventForge.DTOs.Printing.PrinterStatus.Offline;
                }
            }

            return EventForge.DTOs.Printing.PrinterStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing status from QZ response: {Response}", response);
            return EventForge.DTOs.Printing.PrinterStatus.Error;
        }
    }

    private string? ParseJobIdFromResponse(string response, string expectedUid)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            // Check for errors
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorMessage = errorElement.GetString() ?? "Unknown QZ Tray error";
                _logger.LogError("QZ Tray print job returned error: {Error}", errorMessage);
                throw new InvalidOperationException($"QZ Tray error: {errorMessage}");
            }

            // Verify UID matches
            if (document.RootElement.TryGetProperty("uid", out var uidElement))
            {
                var responseUid = uidElement.GetString();
                if (responseUid != expectedUid)
                {
                    _logger.LogWarning("QZ print response UID mismatch. Expected: {Expected}, Got: {Received}", expectedUid, responseUid);
                }
            }

            if (document.RootElement.TryGetProperty("result", out var result))
            {
                // QZ Tray might return a job ID or just success confirmation
                if (result.ValueKind == JsonValueKind.String)
                {
                    return result.GetString();
                }
                else if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("jobId", out var jobIdElement))
                {
                    return jobIdElement.GetString();
                }
                // If no specific job ID, return a generated one based on the UID
                return expectedUid;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing job ID from QZ response: {Response}", response);
            throw;
        }
    }

    private string? ParseVersionFromResponse(string response, string expectedUid)
    {
        try
        {
            using var document = JsonDocument.Parse(response);

            // Check for errors
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorMessage = errorElement.GetString() ?? "Unknown QZ Tray error";
                _logger.LogError("QZ Tray version check returned error: {Error}", errorMessage);
                return null;
            }

            // Verify UID matches
            if (document.RootElement.TryGetProperty("uid", out var uidElement))
            {
                var responseUid = uidElement.GetString();
                if (responseUid != expectedUid)
                {
                    _logger.LogWarning("QZ version response UID mismatch. Expected: {Expected}, Got: {Received}", expectedUid, responseUid);
                }
            }

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

    private object CreatePrintCommand(PrintJobDto printJob, string messageUid)
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
                },
                uid = messageUid
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
                },
                uid = messageUid
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
                },
                uid = messageUid
            }
        };
    }
}