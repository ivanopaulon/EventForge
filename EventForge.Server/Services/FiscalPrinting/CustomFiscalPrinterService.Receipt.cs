using EventForge.DTOs.FiscalPrinting;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class CustomFiscalPrinterService
{
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

        try
        {
            logger.LogInformation(
                "PrintReceiptAsync started for printer {PrinterId} | Items={ItemCount} Payments={PaymentCount}",
                printerId, receipt.Items.Count, receipt.Payments.Count);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);

            var sequence = _builder.BuildFullReceiptSequence(receipt);
            return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in PrintReceiptAsync for printer {PrinterId}.", printerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> CancelCurrentReceiptAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("CancelCurrentReceiptAsync for printer {PrinterId}", printerId);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            byte[] cmd = _builder.BuildCancelReceiptCommand();
            return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CancelCurrentReceiptAsync for printer {PrinterId}.", printerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintRefundReceiptAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refund);

        try
        {
            logger.LogInformation(
                "PrintRefundReceiptAsync for printer {PrinterId} | Original={OriginalReceiptNumber}",
                printerId, refund.OriginalReceiptNumber);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var sequence = _builder.BuildRefundReceiptSequence(refund);
            return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in PrintRefundReceiptAsync for printer {PrinterId}.", printerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintPartialRefundAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refund);

        try
        {
            logger.LogInformation(
                "PrintPartialRefundAsync for printer {PrinterId} | Items={Count}",
                printerId, refund.Items.Count);

            // Partial refund shares the same build logic as full refund
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var sequence = _builder.BuildRefundReceiptSequence(refund);
            return await ExecuteSequenceAsync(channel, sequence, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in PrintPartialRefundAsync for printer {PrinterId}.", printerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> OpenDrawerAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("OpenDrawerAsync for printer {PrinterId}", printerId);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            byte[] cmd = _builder.BuildOpenDrawerCommand();
            return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in OpenDrawerAsync for printer {PrinterId}.", printerId);
            throw;
        }
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
}
