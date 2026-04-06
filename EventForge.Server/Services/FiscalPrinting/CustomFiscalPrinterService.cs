using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.EntityFrameworkCore;

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
}
