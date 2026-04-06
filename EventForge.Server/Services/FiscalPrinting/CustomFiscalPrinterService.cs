using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Net.Sockets;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Implementation of <see cref="IFiscalPrinterService"/> for Custom fiscal printers.
/// Resolves the printer configuration from the database, builds the appropriate
/// communication channel (TCP, serial, or USB-via-Agent proxy), executes the command
/// sequence, and parses the printer responses.
/// </summary>
/// <remarks>
/// Protocol type "Custom" is the only protocol currently supported.
/// The service selects the channel based on <see cref="Printer.ConnectionType"/>:
/// <list type="bullet">
///   <item><term>TCP</term><description>Uses <see cref="CustomTcpCommunication"/> (Address + Port).</description></item>
///   <item><term>Serial</term><description>Uses <see cref="CustomSerialCommunication"/> (SerialPortName).</description></item>
///   <item><term>UsbViaAgent</term><description>Uses <see cref="AgentProxyCommunication"/> (AgentId → URL from configuration, UsbDeviceId).</description></item>
/// </list>
/// Agent base URLs are resolved from <c>AgentProxies:{agentId}</c> in application configuration.
/// </remarks>
public class CustomFiscalPrinterService(
    EventForgeDbContext context,
    ILoggerFactory loggerFactory,
    ILogger<CustomFiscalPrinterService> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IFiscalPrinterService
{
    private readonly FiscalReceiptBuilder _builder = new();

    // Payment method codes recognised as "cash" for POS aggregation
    private const int CashFiscalCode = 1;
    private static readonly HashSet<string> CashPaymentCodes =
        new(StringComparer.OrdinalIgnoreCase) { "CASH", "CONTANTE" };

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

        // USB-via-Agent channel: requires AgentId + UsbDeviceId
        if (printer.ConnectionType == PrinterConnectionType.UsbViaAgent)
        {
            if (printer.AgentId is null)
                throw new InvalidOperationException(
                    $"Printer '{printer.Name}' (ID: {printerId}) is configured as UsbViaAgent but has no AgentId.");

            if (string.IsNullOrWhiteSpace(printer.UsbDeviceId))
                throw new InvalidOperationException(
                    $"Printer '{printer.Name}' (ID: {printerId}) is configured as UsbViaAgent but has no UsbDeviceId.");

            var agentUrl = GetAgentBaseUrl(printer.AgentId.Value);

            logger.LogDebug(
                "Creating agent-proxy channel for printer {Name} (agent={AgentId} device={DeviceId} url={Url})",
                printer.Name, printer.AgentId, printer.UsbDeviceId, agentUrl);

            return new AgentProxyCommunication(
                agentUrl,
                printer.UsbDeviceId,
                timeoutMs: 30_000,
                loggerFactory.CreateLogger<AgentProxyCommunication>(),
                httpClientFactory);
        }

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
            "Set Address+Port for TCP, SerialPortName for serial, or AgentId+UsbDeviceId for USB-via-Agent.");
    }

    /// <summary>
    /// Resolves the base URL of an agent from configuration key <c>AgentProxies:{agentId}</c>.
    /// </summary>
    /// <param name="agentId">The agent's stable GUID.</param>
    /// <returns>The base URL (e.g. <c>http://localhost:5780</c>) of the agent's HTTP endpoint.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no URL is configured for the given <paramref name="agentId"/>.
    /// </exception>
    /// <remarks>
    /// Configure agent URLs in <c>appsettings.json</c> (or environment variables) using the pattern:
    /// <code>
    /// "AgentProxies": {
    ///   "11111111-2222-3333-4444-555555555555": "http://localhost:5780",
    ///   "aaaabbbb-cccc-dddd-eeee-ffffffffffff": "http://192.168.1.50:5780"
    /// }
    /// </code>
    /// Each key is a Guid matching <see cref="Printer.AgentId"/>; the value is the agent's HTTP base URL.
    /// </remarks>
    private string GetAgentBaseUrl(Guid agentId)
    {
        var url = configuration[$"AgentProxies:{agentId}"];

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                $"No base URL configured for agent '{agentId}'. " +
                $"Add 'AgentProxies:{agentId}' to application configuration.");

        return url.TrimEnd('/');
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
    //  Daily closure workflow  (Sprint 5C – DB-backed)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<DailyClosurePreCheckDto> GetDailyClosurePreCheckAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        var preCheck = new DailyClosurePreCheckDto();

        try
        {
            // Attempt a status read to detect open-receipt and drawer states
            var statusResult = await GetStatusAsync(printerId, cancellationToken);
            preCheck.HasOpenReceipt = statusResult.IsReceiptOpen;
            preCheck.IsDrawerOpen = statusResult.IsDrawerOpen;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetDailyClosurePreCheckAsync: could not read status for printer {PrinterId}", printerId);
        }

        // Load last closure date from DB
        var lastClosure = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.PrinterId == printerId && !r.IsDeleted)
            .OrderByDescending(r => r.ClosedAt)
            .Select(r => (DateTime?)r.ClosedAt)
            .FirstOrDefaultAsync(cancellationToken);

        preCheck.LastClosureDate = lastClosure;
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

        // Read tenant id from printer
        var printer = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name, p.TenantId })
            .FirstOrDefaultAsync(cancellationToken);

        var zNumber = int.TryParse(printResult.ReceiptNumber, out var n) ? n : 0;
        var closedAt = DateTime.UtcNow;

        // Aggregate POS data: find all SaleSession closed today on POSes linked to this printer
        var todayStart = closedAt.Date;
        var posIds = await context.StorePoses
            .AsNoTracking()
            .Where(p => p.DefaultFiscalPrinterId == printerId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        decimal totalAmount = 0m, cashAmount = 0m, cardAmount = 0m;
        int receiptCount = 0;

        if (posIds.Count > 0)
        {
            var sessions = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Where(s => posIds.Contains(s.PosId)
                         && !s.IsDeleted
                         && s.Status == EventForge.Server.Data.Entities.Sales.SaleSessionStatus.Closed
                         && s.ClosedAt.HasValue && s.ClosedAt.Value.Date == todayStart)
                .ToListAsync(cancellationToken);

            receiptCount = sessions.Count;
            totalAmount = sessions.Sum(s => s.FinalTotal);

            foreach (var session in sessions)
            {
                foreach (var payment in session.Payments)
                {
                    var code = payment.PaymentMethod?.Code?.ToUpperInvariant() ?? string.Empty;
                    // FiscalCode 1 = cash; any other recognised code = card/electronic
                    if (payment.PaymentMethod?.FiscalCode == CashFiscalCode
                        || CashPaymentCodes.Contains(code))
                        cashAmount += payment.Amount;
                    else
                        cardAmount += payment.Amount;
                }
            }
        }

        var record = new Data.Entities.FiscalPrinting.DailyClosureRecord
        {
            PrinterId = printerId,
            TenantId = printer?.TenantId ?? Guid.Empty,
            ZReportNumber = zNumber,
            ClosedAt = closedAt,
            ReceiptCount = receiptCount,
            TotalAmount = totalAmount,
            CashAmount = cashAmount,
            CardAmount = cardAmount,
            Operator = operatorName,
            HasPdf = false,
            PrinterResponse = null,
            CreatedBy = operatorName
        };

        context.DailyClosureRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "DailyClosure saved to DB | PrinterId={PrinterId} ClosureId={ClosureId} ZReport={Z} Operator={Op}",
            printerId, record.Id, zNumber, operatorName);

        return new DailyClosureResultDto
        {
            Success = true,
            ClosureId = record.Id,
            ZReportNumber = zNumber,
            ClosedAt = closedAt,
            ReceiptCount = record.ReceiptCount,
            TotalAmount = record.TotalAmount,
            Operator = operatorName
        };
    }

    /// <inheritdoc />
    public async Task<List<DailyClosureHistoryDto>> GetClosureHistoryAsync(
        Guid printerId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.PrinterId == printerId && !r.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(r => r.ClosedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.ClosedAt <= toDate.Value);

        var printerName = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? printerId.ToString();

        var records = await query
            .OrderByDescending(r => r.ClosedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return records.Select(r => new DailyClosureHistoryDto
        {
            Id = r.Id,
            PrinterId = r.PrinterId,
            PrinterName = printerName,
            ZReportNumber = r.ZReportNumber,
            ClosedAt = r.ClosedAt,
            ReceiptCount = r.ReceiptCount,
            TotalAmount = r.TotalAmount,
            CashAmount = r.CashAmount,
            CardAmount = r.CardAmount,
            Operator = r.Operator,
            HasPdf = r.HasPdf
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        var record = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = $"Closure {closureId} not found",
                PrintDate = DateTime.UtcNow
            };
        }

        logger.LogInformation("ReprintZReportAsync | ClosureId={ClosureId} PrinterId={PrinterId}", closureId, record.PrinterId);

        // The Custom protocol reprint command is the standard DailyClosure command
        return await DailyClosureAsync(record.PrinterId, cancellationToken);
    }

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
