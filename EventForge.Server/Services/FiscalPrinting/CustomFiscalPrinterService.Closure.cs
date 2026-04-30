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

        // Attempt a live connection to determine printer reachability and receipt/drawer state
        try
        {
            var statusResult = await GetStatusAsync(printerId, cancellationToken);
            preCheck.HasOpenReceipt = statusResult.IsReceiptOpen;
            preCheck.IsDrawerOpen = statusResult.IsDrawerOpen;
            preCheck.PrinterAvailable = statusResult.IsOnline;
            if (!statusResult.IsOnline)
            {
                preCheck.PrinterReachabilityError = statusResult.LastError ?? "Stampante non raggiungibile";
                preCheck.PlannedClosureType = ClosureType.SoloDatabase;
            }
            else
            {
                preCheck.PlannedClosureType = ClosureType.Fiscale;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetDailyClosurePreCheckAsync: could not read status for printer {PrinterId}", printerId);
            preCheck.PrinterAvailable = false;
            preCheck.PrinterReachabilityError = ex.Message;
            preCheck.PlannedClosureType = ClosureType.SoloDatabase;
        }

        // Load last closure date from DB
        var lastClosure = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.PrinterId == printerId && !r.IsDeleted)
            .OrderByDescending(r => r.ClosedAt)
            .Select(r => (DateTime?)r.ClosedAt)
            .FirstOrDefaultAsync(cancellationToken);

        preCheck.LastClosureDate = lastClosure;

        // Aggregate today's session totals from DB for the summary
        var todayStart = DateTime.UtcNow.Date;
        var posIds = await context.StorePoses
            .AsNoTracking()
            .Where(p => p.DefaultFiscalPrinterId == printerId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

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

            preCheck.ReceiptCount = sessions.Count;
            preCheck.TotalAmount = sessions.Sum(s => s.FinalTotal);

            foreach (var session in sessions)
                foreach (var payment in session.Payments)
                {
                    var code = payment.PaymentMethod?.Code?.ToUpperInvariant() ?? string.Empty;
                    if (payment.PaymentMethod?.FiscalCode == CashFiscalCode || CashPaymentCodes.Contains(code))
                        preCheck.CashAmount += payment.Amount;
                    else
                        preCheck.CardAmount += payment.Amount;
                }
        }

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

            // Try to execute the hardware Z-closure first; capture any printer errors
            FiscalPrintResult? printResult = null;
            string? printerError = null;
            try
            {
                printResult = await DailyClosureAsync(printerId, cancellationToken);
                if (!printResult.Success)
                    printerError = printResult.ErrorMessage;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Custom ExecuteDailyClosureAsync: printer unreachable, proceeding with DB-only closure | PrinterId={PrinterId}",
                    printerId);
                printerError = ex.Message;
            }

            bool fiscalPending = printerError is not null;

            // Read tenant id from printer
            var printer = await context.Printers
                .AsNoTracking()
                .Where(p => p.Id == printerId && !p.IsDeleted)
                .Select(p => new { p.Id, p.Name, p.TenantId })
                .FirstOrDefaultAsync(cancellationToken);

            // Get next Z-number by incrementing the last one from DB.
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
                        if (payment.PaymentMethod?.FiscalCode == CashFiscalCode
                            || CashPaymentCodes.Contains(code))
                            cashAmount += payment.Amount;
                        else
                            cardAmount += payment.Amount;
                    }
                }
            }

            var closureType = fiscalPending ? "SoloDatabase" : "Fiscale";
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
                ClosureType = closureType,
                FiscalClosurePending = fiscalPending,
                PrinterErrors = fiscalPending ? printerError?[..Math.Min(printerError.Length, 500)] : null,
                HasPdf = false,
                PrinterResponse = null,
                CreatedBy = operatorName
            };

            context.DailyClosureRecords.Add(record);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Custom DailyClosure saved | PrinterId={PrinterId} ClosureId={ClosureId} Z={Z} Type={Type} Pending={Pending} Operator={Op}",
                printerId, record.Id, zNumber, closureType, fiscalPending, operatorName);

            return new DailyClosureResultDto
            {
                Success = true,
                ClosureId = record.Id,
                ZReportNumber = zNumber,
                ClosedAt = closedAt,
                ReceiptCount = record.ReceiptCount,
                TotalAmount = record.TotalAmount,
                CashAmount = record.CashAmount,
                Operator = operatorName,
                ClosureType = fiscalPending ? ClosureType.SoloDatabase : ClosureType.Fiscale,
                FiscalClosurePending = fiscalPending,
                PrinterErrors = record.PrinterErrors
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Custom ExecuteDailyClosureAsync failed | PrinterId={PrinterId}", printerId);
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
                HasPdf = r.HasPdf,
                ClosureType = Enum.TryParse<ClosureType>(r.ClosureType, out var ct) ? ct : ClosureType.Fiscale,
                FiscalClosurePending = r.FiscalClosurePending
            }).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Not supported on individual printer services; always handled by <see cref="FiscalPrinterServiceRouter"/>.
    /// </remarks>
    public Task<List<DailyClosureHistoryDto>> GetAllClosureHistoryAsync(
        int page = 1, int pageSize = 50,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "GetAllClosureHistoryAsync must be called on FiscalPrinterServiceRouter, not on a specific printer service.");

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

    /// <inheritdoc />
    public async Task<DailyClosureResultDto> RetryFiscalClosureAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        var record = await context.DailyClosureRecords
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return new DailyClosureResultDto { Success = false, ErrorMessage = $"Closure record {closureId} not found." };

        if (!record.FiscalClosurePending)
            return new DailyClosureResultDto { Success = false, ErrorMessage = "La chiusura fiscale per questo record non è pendente." };

        logger.LogInformation("Custom RetryFiscalClosureAsync | ClosureId={ClosureId} PrinterId={PrinterId}", closureId, record.PrinterId);

        try
        {
            var printResult = await DailyClosureAsync(record.PrinterId, cancellationToken);

            if (printResult.Success)
            {
                record.FiscalClosurePending = false;
                record.ClosureType = "Fiscale";
                record.PrinterErrors = null;
                record.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                return new DailyClosureResultDto
                {
                    Success = true,
                    ClosureId = record.Id,
                    ZReportNumber = record.ZReportNumber,
                    ClosedAt = record.ClosedAt,
                    ReceiptCount = record.ReceiptCount,
                    TotalAmount = record.TotalAmount,
                    CashAmount = record.CashAmount,
                    Operator = record.Operator,
                    ClosureType = ClosureType.Fiscale,
                    FiscalClosurePending = false
                };
            }
            else
            {
                record.PrinterErrors = printResult.ErrorMessage?[..Math.Min(printResult.ErrorMessage.Length, 500)];
                record.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                return new DailyClosureResultDto
                {
                    Success = false,
                    ErrorMessage = printResult.ErrorMessage,
                    ClosureId = record.Id,
                    ClosureType = ClosureType.SoloDatabase,
                    FiscalClosurePending = true,
                    PrinterErrors = printResult.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Custom RetryFiscalClosureAsync: printer still unreachable | ClosureId={ClosureId}", closureId);
            record.PrinterErrors = ex.Message[..Math.Min(ex.Message.Length, 500)];
            record.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return new DailyClosureResultDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ClosureId = record.Id,
                ClosureType = ClosureType.SoloDatabase,
                FiscalClosurePending = true,
                PrinterErrors = ex.Message
            };
        }
    }
}
