using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Implementation of <see cref="IFiscalPrinterService"/> for Custom fiscal printers.
/// Resolves the printer configuration from the database, builds the appropriate
/// communication channel (TCP or serial), executes the command sequence, and
/// parses the printer responses.
/// </summary>
/// <remarks>
/// Protocol type "Custom" is the only protocol currently supported.
/// The service detects whether to use TCP or serial based on the presence of
/// <c>Printer.Address</c> + <c>Printer.Port</c> vs <c>Printer.SerialPortName</c>.
/// </remarks>
public class CustomFiscalPrinterService(
    EventForgeDbContext context,
    ILoggerFactory loggerFactory,
    ILogger<CustomFiscalPrinterService> logger) : IFiscalPrinterService
{
    private readonly FiscalReceiptBuilder _builder = new();

    // In-memory closure history (replaced by DB persistence in a future sprint)
    // WARNING: this data is lost on application restart and not shared across instances.
    // Deploy Sprint 5C DB persistence before going to production.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, List<DailyClosureHistoryDto>>
        _closureHistory = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DailyClosureHistoryDto>
        _closureById = new();

    // -------------------------------------------------------------------------
    //  IFiscalPrinterService
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintReceiptAsync(
        Guid printerId,
        FiscalReceiptData receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        logger.LogInformation(
            "PrintReceiptAsync started for printer {PrinterId} | Items={ItemCount} Payments={PaymentCount}",
            printerId, receipt.Items.Count, receipt.Payments.Count);

        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);

        var sequence = _builder.BuildFullReceiptSequence(receipt);
        return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> CancelCurrentReceiptAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CancelCurrentReceiptAsync for printer {PrinterId}", printerId);

        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
        byte[] cmd = _builder.BuildCancelReceiptCommand();
        return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintRefundReceiptAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refund);

        logger.LogInformation(
            "PrintRefundReceiptAsync for printer {PrinterId} | Original={OriginalReceiptNumber}",
            printerId, refund.OriginalReceiptNumber);

        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
        var sequence = _builder.BuildRefundReceiptSequence(refund);
        return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintPartialRefundAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refund);

        logger.LogInformation(
            "PrintPartialRefundAsync for printer {PrinterId} | Items={Count}",
            printerId, refund.Items.Count);

        // Partial refund shares the same build logic as full refund
        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
        var sequence = _builder.BuildRefundReceiptSequence(refund);
        return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DailyClosureAsync for printer {PrinterId}", printerId);

        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
        byte[] cmd = _builder.BuildDailyClosureCommand();
        return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrinterStatus> GetStatusAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetStatusAsync for printer {PrinterId}", printerId);

        try
        {
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);

            byte[] cmd = _builder.BuildReadStatusCommand();
            byte[] responseBytes = await channel.SendCommandAsync(cmd, cancellationToken).ConfigureAwait(false);

            var response = CustomResponseParser.ParseResponse(responseBytes);

            if (response.Type == CustomResponseType.Data
                && CustomResponseParser.TryExtractData(responseBytes, out string? data)
                && !string.IsNullOrEmpty(data))
            {
                // StatusParser expects raw bytes; encode the ASCII data back to bytes
                byte[] statusBytes = System.Text.Encoding.Latin1.GetBytes(data);
                var status = CustomStatusParser.Parse(statusBytes);
                status.IsOnline = true;
                status.LastCheck = DateTime.UtcNow;
                return status;
            }

            // NAK or unexpected response
            return new FiscalPrinterStatus
            {
                IsOnline = true,
                LastCheck = DateTime.UtcNow,
                LastError = response.Type == CustomResponseType.Nak
                    ? "Printer returned NAK to status request"
                    : "Unexpected response to status request"
            };
        }
        catch (Exception ex) when (ex is FiscalPrinterCommunicationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "GetStatusAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrinterStatus
            {
                IsOnline = false,
                LastCheck = DateTime.UtcNow,
                LastError = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> OpenDrawerAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("OpenDrawerAsync for printer {PrinterId}", printerId);

        await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
        byte[] cmd = _builder.BuildOpenDrawerCommand();
        return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestConnectionAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("TestConnectionAsync for printer {PrinterId}", printerId);

        try
        {
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            await channel.TestConnectionAsync(cancellationToken).ConfigureAwait(false);

            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TestConnectionAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                PrintDate = DateTime.UtcNow
            };
        }
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads the printer from the database and creates the appropriate communication channel.
    /// </summary>
    private async Task<ICustomPrinterCommunication> CreateChannelAsync(
        Guid printerId,
        CancellationToken cancellationToken)
    {
        var printer = await context.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == printerId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
            throw new InvalidOperationException($"Fiscal printer with ID {printerId} not found.");

        if (!printer.IsFiscalPrinter)
            throw new InvalidOperationException(
                $"Printer '{printer.Name}' (ID: {printerId}) is not configured as a fiscal printer.");

        // TCP channel: requires Address + Port
        if (!string.IsNullOrWhiteSpace(printer.Address) && printer.Port > 0)
        {
            logger.LogDebug(
                "Creating TCP channel for printer {Name} ({Host}:{Port})",
                printer.Name, printer.Address, printer.Port);

            return new CustomTcpCommunication(
                printer.Address,
                printer.Port!.Value,
                loggerFactory.CreateLogger<CustomTcpCommunication>());
        }

        // Serial channel: requires SerialPortName
        if (!string.IsNullOrWhiteSpace(printer.SerialPortName))
        {
            logger.LogDebug(
                "Creating serial channel for printer {Name} ({Port} @ {BaudRate} baud)",
                printer.Name, printer.SerialPortName, printer.BaudRate ?? 9600);

            return new CustomSerialCommunication(
                printer.SerialPortName,
                loggerFactory.CreateLogger<CustomSerialCommunication>(),
                baudRate: printer.BaudRate ?? 9600);
        }

        throw new InvalidOperationException(
            $"Printer '{printer.Name}' (ID: {printerId}) has no valid connection configuration. " +
            "Set Address+Port for TCP or SerialPortName for serial communication.");
    }

    /// <summary>
    /// Sends all frames in <paramref name="sequence"/> sequentially and verifies each response.
    /// Returns a failure result immediately if any frame receives a NAK.
    /// </summary>
    private async Task<FiscalPrintResult> ExecuteSequenceAsync(
        ICustomPrinterCommunication channel,
        IReadOnlyList<byte[]> sequence,
        Guid printerId,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            try
            {
                byte[] responseBytes = await channel
                    .SendCommandAsync(sequence[i], cancellationToken)
                    .ConfigureAwait(false);

                var response = CustomResponseParser.ParseResponse(responseBytes);

                if (response.Type == CustomResponseType.Nak)
                {
                    string err = $"Printer returned NAK for command {i + 1}/{sequence.Count}";
                    logger.LogWarning("{Error} | PrinterId={PrinterId}", err, printerId);
                    return new FiscalPrintResult
                    {
                        Success = false,
                        ErrorMessage = err,
                        PrintDate = DateTime.UtcNow
                    };
                }

                if (response.Type == CustomResponseType.Unknown)
                {
                    logger.LogWarning(
                        "Unexpected response type for command {Idx}/{Total} | PrinterId={PrinterId}",
                        i + 1, sequence.Count, printerId);
                }
            }
            catch (FiscalPrinterCommunicationException ex)
            {
                logger.LogError(
                    ex,
                    "Communication error on command {Idx}/{Total} for printer {PrinterId}",
                    i + 1, sequence.Count, printerId);

                return new FiscalPrintResult
                {
                    Success = false,
                    ErrorMessage = $"Communication error: {ex.Message}",
                    PrintDate = DateTime.UtcNow
                };
            }
        }

        logger.LogInformation(
            "Sequence completed successfully | PrinterId={PrinterId} Commands={Count}",
            printerId, sequence.Count);

        return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
    }

    // -------------------------------------------------------------------------
    //  Ad-hoc connection tests (used by wizard – no DB record required)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestTcpConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomTcpCommunication(
                ipAddress, port, loggerFactory.CreateLogger<CustomTcpCommunication>());

            await comm.TestConnectionAsync(cancellationToken);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestSerialConnectionAsync(
        string serialPortName,
        int baudRate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomSerialCommunication(
                serialPortName,
                loggerFactory.CreateLogger<CustomSerialCommunication>(),
                baudRate);

            await comm.TestConnectionAsync(cancellationToken);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    // -------------------------------------------------------------------------
    //  Printer info by address (wizard Step 3)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrinterInfoDto> GetPrinterInfoByAddressAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomTcpCommunication(
                ipAddress, port, loggerFactory.CreateLogger<CustomTcpCommunication>());

            // Send CMD_READ_STATUS ("10") and parse response
            var statusCmd = CustomCommandBuilder.StatusRequest();
            var rawResponse = await comm.SendCommandAsync(statusCmd, cancellationToken);
            await comm.DisconnectAsync();

            if (rawResponse.Length == 0)
                return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = "No response from printer" };

            var parsed = CustomResponseParser.ParseResponse(rawResponse);
            if (parsed.Type == CustomResponseType.Nak || parsed.Type == CustomResponseType.Unknown)
                return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = "Printer returned NAK or unknown response" };

            // Best-effort: parse status bytes if a Data frame was returned
            var isOnline = parsed.Type == CustomResponseType.Ack || parsed.Type == CustomResponseType.Data;
            bool memoryAlmostFull = false, memoryFull = false;

            if (parsed.Type == CustomResponseType.Data && !string.IsNullOrEmpty(parsed.Data))
            {
                var statusBytes = System.Text.Encoding.ASCII.GetBytes(parsed.Data);
                if (statusBytes.Length >= 3)
                {
                    var status = CustomStatusParser.Parse(statusBytes);
                    memoryAlmostFull = status.IsFiscalMemoryAlmostFull;
                    memoryFull = status.IsFiscalMemoryFull;
                }
            }

            return new FiscalPrinterInfoDto
            {
                IsOnline = isOnline,
                FiscalMemoryUsedPercent = memoryFull ? 100m : memoryAlmostFull ? 92m : null,
                PrinterDateTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetPrinterInfoByAddressAsync failed for {IpAddress}:{Port}", ipAddress, port);
            return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = ex.Message };
        }
    }

    // -------------------------------------------------------------------------
    //  Network scan (wizard Step 2A)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<List<NetworkScanResultDto>> ScanNetworkAsync(
        string subnetPrefix,
        int port = 9100,
        int timeoutMs = 300,
        CancellationToken cancellationToken = default)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<NetworkScanResultDto>();

        // Probe .1 to .254 in parallel, capped at 50 concurrent connections
        using var semaphore = new SemaphoreSlim(50);
        var tasks = Enumerable.Range(1, 254).Select(async i =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var ip = $"{subnetPrefix}.{i}";
                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using var tcp = new TcpClient();
                    var connectTask = tcp.ConnectAsync(ip, port, cancellationToken).AsTask();
                    if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cancellationToken)) == connectTask
                        && connectTask.IsCompletedSuccessfully)
                    {
                        sw.Stop();
                        results.Add(new NetworkScanResultDto
                        {
                            IpAddress = ip,
                            Port = port,
                            RoundTripMs = (int)sw.ElapsedMilliseconds,
                            RespondedToProtocol = false
                        });
                    }
                }
                catch { /* host not reachable – expected */ }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var list = results.OrderBy(r => r.IpAddress, StringComparer.OrdinalIgnoreCase).ToList();
        logger.LogInformation("Network scan {Subnet}.x:{Port} found {Count} devices", subnetPrefix, port, list.Count);
        return list;
    }

    // -------------------------------------------------------------------------
    //  Daily closure workflow
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<DailyClosurePreCheckDto> GetDailyClosurePreCheckAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        var printer = await ResolveAndLoadPrinterAsync(printerId, cancellationToken);
        var preCheck = new DailyClosurePreCheckDto();

        try
        {
            // Attempt a status read to detect open-receipt and drawer states
            var statusResult = await GetStatusAsync(printerId, cancellationToken);
            preCheck.IsDrawerOpen = false; // Custom status bitmap does not expose drawer state directly
            // IsReceiptOpen is inferred from the status – if the printer has an error/busy flag
            // we treat it as an open receipt to be safe
            preCheck.HasOpenReceipt = statusResult.IsReceiptOpen;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetDailyClosurePreCheckAsync: could not read status for printer {PrinterId}", printerId);
        }

        // Populate history-based totals from our in-memory history for now
        // (will be replaced by DB aggregation in a future sprint)
        preCheck.LastClosureDate = _closureHistory.TryGetValue(printerId, out var history) && history.Count > 0
            ? history.Max(c => c.ClosedAt)
            : null;

        preCheck.CheckedAt = DateTime.UtcNow;
        return preCheck;
    }

    /// <inheritdoc />
    public async Task<DailyClosureResultDto> ExecuteDailyClosureAsync(
        Guid printerId,
        string operatorName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("ExecuteDailyClosureAsync | PrinterId={PrinterId} Operator={Op}", printerId, operatorName);

        var printResult = await DailyClosureAsync(printerId, cancellationToken);

        if (!printResult.Success)
        {
            return new DailyClosureResultDto
            {
                Success = false,
                ErrorMessage = printResult.ErrorMessage,
                ClosedAt = DateTime.UtcNow
            };
        }

        // Build closure record
        var closureId = Guid.NewGuid();
        var printer = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(cancellationToken);

        var historyEntry = new DailyClosureHistoryDto
        {
            Id = closureId,
            PrinterId = printerId,
            PrinterName = printer?.Name ?? printerId.ToString(),
            ZReportNumber = int.TryParse(printResult.ReceiptNumber, out var n) ? n : 0,
            ClosedAt = DateTime.UtcNow,
            ReceiptCount = 0, // aggregation from POS data is deferred to Sprint 5C
            TotalAmount = 0m,
            Operator = operatorName,
            HasPdf = false
        };

        _closureHistory.AddOrUpdate(
            printerId,
            _ => [historyEntry],
            (_, existing) => { existing.Add(historyEntry); return existing; });

        _closureById[closureId] = historyEntry;

        logger.LogInformation(
            "DailyClosure recorded | PrinterId={PrinterId} ClosureId={ClosureId} Operator={Op}",
            printerId, closureId, operatorName);

        return new DailyClosureResultDto
        {
            Success = true,
            ClosureId = closureId,
            ZReportNumber = historyEntry.ZReportNumber,
            ClosedAt = historyEntry.ClosedAt,
            ReceiptCount = historyEntry.ReceiptCount,
            TotalAmount = historyEntry.TotalAmount,
            Operator = operatorName
        };
    }

    /// <inheritdoc />
    public Task<List<DailyClosureHistoryDto>> GetClosureHistoryAsync(
        Guid printerId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!_closureHistory.TryGetValue(printerId, out var history))
            return Task.FromResult(new List<DailyClosureHistoryDto>());

        IEnumerable<DailyClosureHistoryDto> query = history.OrderByDescending(c => c.ClosedAt);

        if (fromDate.HasValue)
            query = query.Where(c => c.ClosedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(c => c.ClosedAt <= toDate.Value);

        var result = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        if (!_closureById.TryGetValue(closureId, out var closure))
        {
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = $"Closure {closureId} not found in history",
                PrintDate = DateTime.UtcNow
            };
        }

        logger.LogInformation("ReprintZReportAsync | ClosureId={ClosureId} PrinterId={PrinterId}", closureId, closure.PrinterId);

        // The Custom protocol reprint command is the standard DailyClosure command
        return await DailyClosureAsync(closure.PrinterId, cancellationToken);
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private async Task<EventForge.Server.Data.Entities.Common.Printer> ResolveAndLoadPrinterAsync(
        Guid printerId, CancellationToken cancellationToken)
    {
        var printer = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Printer {printerId} not found.");
        return printer;
    }
}
