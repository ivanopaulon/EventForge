using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class CustomFiscalPrinterService
{
    /// <inheritdoc />
    /// <remarks>DB-only operation – implemented in <see cref="FiscalPrinterServiceRouter"/>; not delegated here.</remarks>
    public Task<PreviousDayClosureStatusDto> GetPreviousDayClosureStatusAsync(
        Guid printerId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "GetPreviousDayClosureStatusAsync must be called through FiscalPrinterServiceRouter.");

    /// <inheritdoc />
    public async Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("DailyClosureAsync for printer {PrinterId}", printerId);

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            byte[] cmd = _builder.BuildDailyClosureCommand();
            return await ExecuteSequenceAsync(channel, [cmd], printerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw;
        }
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
        catch (InvalidOperationException ex)
        {
            // Configuration error (e.g. missing UsbDeviceId) — log at Debug to avoid
            // flooding logs on every poll cycle; the monitor surfaces it via status.LastError.
            logger.LogDebug(ex, "GetStatusAsync failed for printer {PrinterId} (configuration error)", printerId);
            return new FiscalPrinterStatus
            {
                IsOnline = false,
                LastCheck = DateTime.UtcNow,
                LastError = ex.Message
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
        try
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

            // Get next Z-number by incrementing the last one from DB.
            // printResult.ReceiptNumber is not populated by the Custom protocol response
            // and therefore cannot be relied upon for Z-numbering.
            var lastZNumber = await context.DailyClosureRecords
                .AsNoTracking()
                .Where(r => r.PrinterId == printerId && !r.IsDeleted)
                .OrderByDescending(r => r.ZReportNumber)
                .Select(r => (int?)r.ZReportNumber)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            var zNumber = lastZNumber + 1;
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
        catch (Exception ex)
        {
            throw;
        }
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
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The Custom protocol does not have a dedicated reprint-Z command.
    /// This method prints a descriptive summary of the stored closure using
    /// non-fiscal <c>CMD_PRINT_DESCRIPTIVE</c> ("20") lines so that the operator
    /// gets a paper copy without triggering a new hardware Z-closure.
    /// </remarks>
    public async Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        try
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

            logger.LogInformation(
                "Custom ReprintZReportAsync | ClosureId={ClosureId} PrinterId={PrinterId}",
                closureId, record.PrinterId);

            var closureDto = new DailyClosureResultDto
            {
                Success = true,
                ClosureId = record.Id,
                ZReportNumber = record.ZReportNumber,
                ClosedAt = record.ClosedAt,
                ReceiptCount = record.ReceiptCount,
                TotalAmount = record.TotalAmount,
                CashAmount = record.CashAmount,
                Operator = record.Operator
            };

            await using var channel = await CreateChannelAsync(record.PrinterId, cancellationToken).ConfigureAwait(false);
            var sequence = _builder.BuildPrintZReportSummarySequence(closureDto);
            return await ExecuteSequenceAsync(channel, sequence, record.PrinterId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Custom ReprintZReportAsync failed | ClosureId={ClosureId}", closureId);
            throw;
        }
    }
}
