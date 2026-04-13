using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class EpsonFiscalPrinterService
{
    // -------------------------------------------------------------------------
    //  IFiscalPrinterService – receipt operations
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
                "Epson PrintReceiptAsync | PrinterId={PrinterId} Items={Items} Payments={Payments}",
                printerId, receipt.Items.Count, receipt.Payments.Count);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildFiscalReceipt(
                receipt, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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
            logger.LogInformation("Epson CancelCurrentReceiptAsync | PrinterId={PrinterId}", printerId);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildCancelReceipt(channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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
                "Epson PrintRefundReceiptAsync | PrinterId={PrinterId} Original={OriginalReceiptNumber}",
                printerId, refund.OriginalReceiptNumber);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildRefundReceipt(
                refund, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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
                "Epson PrintPartialRefundAsync | PrinterId={PrinterId} Items={Count}",
                printerId, refund.Items.Count);

            // Partial and full refunds use the same XML builder
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildRefundReceipt(
                refund, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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
            logger.LogInformation("Epson OpenDrawerAsync | PrinterId={PrinterId}", printerId);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildOpenDrawer(channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestConnectionAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Epson TestConnectionAsync | PrinterId={PrinterId}", printerId);

        try
        {
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            await channel.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Epson TestConnectionAsync failed | PrinterId={PrinterId}", printerId);
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                PrintDate = DateTime.UtcNow
            };
        }
    }
}
