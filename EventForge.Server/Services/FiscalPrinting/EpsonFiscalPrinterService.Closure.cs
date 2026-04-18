using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class EpsonFiscalPrinterService
{
    // -------------------------------------------------------------------------
    //  IFiscalPrinterService – daily closure
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>DB-only operation – implemented in <see cref="FiscalPrinterServiceRouter"/>; not delegated here.</remarks>
    public Task<PreviousDayClosureStatusDto> GetPreviousDayClosureStatusAsync(
        Guid printerId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "GetPreviousDayClosureStatusAsync must be called through FiscalPrinterServiceRouter.");

    /// <inheritdoc />
    /// <remarks>
    /// For Epson TM-series receipt printers there is no hardware Z-closure command.
    /// <see cref="DailyClosureAsync"/> prints a Z-report summary document on the
    /// printer and returns success. Use <see cref="ExecuteDailyClosureAsync"/> to
    /// also record the closure in the EventForge database.
    /// </remarks>
    public async Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Epson DailyClosureAsync | PrinterId={PrinterId}", printerId);

            // Build a minimal closure DTO from DB for the Z-report document
            var lastClosure = await context.DailyClosureRecords
                .AsNoTracking()
                .Where(r => r.PrinterId == printerId && !r.IsDeleted)
                .OrderByDescending(r => r.ClosedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var closureDto = lastClosure is not null
                ? new DailyClosureResultDto
                {
                    Success = true,
                    ClosureId = lastClosure.Id,
                    ZReportNumber = lastClosure.ZReportNumber,
                    ClosedAt = lastClosure.ClosedAt,
                    ReceiptCount = lastClosure.ReceiptCount,
                    TotalAmount = lastClosure.TotalAmount,
                    CashAmount = lastClosure.CashAmount,
                    Operator = lastClosure.Operator
                }
                : new DailyClosureResultDto
                {
                    Success = true,
                    ClosedAt = DateTime.UtcNow
                };

            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildZReport(closureDto, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);
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
        logger.LogDebug("Epson GetStatusAsync | PrinterId={PrinterId}", printerId);

        try
        {
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildStatusQuery(channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            var rawResponse = await channel.SendXmlAsync(xml, cancellationToken).ConfigureAwait(false);
            var status = EpsonResponseParser.ParseStatusResponse(rawResponse);
            return status;
        }
        catch (Exception ex) when (ex is FiscalPrinterCommunicationException
                                       or InvalidOperationException
                                       or OperationCanceledException)
        {
            logger.LogWarning(ex, "Epson GetStatusAsync failed | PrinterId={PrinterId}", printerId);
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
            var statusResult = await GetStatusAsync(printerId, cancellationToken);
            preCheck.IsDrawerOpen = statusResult.IsDrawerOpen;
            // Epson receipt printers don't have an open receipt concept
            preCheck.HasOpenReceipt = false;
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
            logger.LogWarning(ex,
                "Epson GetDailyClosurePreCheckAsync: could not read status | PrinterId={PrinterId}", printerId);
            preCheck.PrinterAvailable = false;
            preCheck.PrinterReachabilityError = ex.Message;
            preCheck.PlannedClosureType = ClosureType.SoloDatabase;
        }

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
        logger.LogInformation(
            "Epson ExecuteDailyClosureAsync | PrinterId={PrinterId} Operator={Op}",
            printerId, operatorName);

        var closedAt = DateTime.UtcNow;
        var todayStart = closedAt.Date;

        // Aggregate POS data: find all SaleSession closed today on POSes linked to this printer
        var posIds = await context.StorePoses
            .AsNoTracking()
            .Where(p => p.DefaultFiscalPrinterId == printerId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        decimal totalAmount = 0m, cashAmount = 0m;
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
                }
            }
        }

        // Get next Z-number (increment from last closure)
        var lastZNumber = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.PrinterId == printerId && !r.IsDeleted)
            .OrderByDescending(r => r.ZReportNumber)
            .Select(r => (int?)r.ZReportNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        int zNumber = lastZNumber + 1;

        // Load TenantId from the printer entity for the closure record
        var tenantId = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .Select(p => (Guid?)p.TenantId)
            .FirstOrDefaultAsync(cancellationToken) ?? Guid.Empty;

        var record = new Data.Entities.FiscalPrinting.DailyClosureRecord
        {
            PrinterId = printerId,
            TenantId = tenantId,
            ZReportNumber = zNumber,
            ClosedAt = closedAt,
            ReceiptCount = receiptCount,
            TotalAmount = totalAmount,
            CashAmount = cashAmount,
            CardAmount = totalAmount - cashAmount,
            Operator = operatorName,
            ClosureType = "Fiscale",
            FiscalClosurePending = false,
            HasPdf = false,
            PrinterResponse = null,
            CreatedBy = operatorName
        };

        context.DailyClosureRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        var closureDto = new DailyClosureResultDto
        {
            Success = true,
            ClosureId = record.Id,
            ZReportNumber = zNumber,
            ClosedAt = closedAt,
            ReceiptCount = receiptCount,
            TotalAmount = totalAmount,
            CashAmount = cashAmount,
            Operator = operatorName
        };

        // Print the Z-report document on the Epson printer
        bool fiscalPending = false;
        string? printerError = null;
        try
        {
            await using var channel = await CreateChannelAsync(printerId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildZReport(
                closureDto, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            var printResult = await ExecuteXmlAsync(channel, xml, printerId, cancellationToken).ConfigureAwait(false);

            if (!printResult.Success)
            {
                logger.LogWarning(
                    "Epson Z-report printed but printer returned a warning | PrinterId={PrinterId} Msg={Msg}",
                    printerId, printResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // The DB closure record is already saved; mark fiscal part as pending
            logger.LogWarning(ex,
                "Epson ExecuteDailyClosureAsync: closure saved but Z-report print failed | PrinterId={PrinterId}",
                printerId);
            fiscalPending = true;
            printerError = ex.Message;
        }

        // Persist fiscal-pending state and closure type
        record.ClosureType = fiscalPending ? "SoloDatabase" : "Fiscale";
        record.FiscalClosurePending = fiscalPending;
        record.PrinterErrors = fiscalPending ? printerError?[..Math.Min(printerError.Length, 500)] : null;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Epson DailyClosure saved | PrinterId={PrinterId} ClosureId={ClosureId} Z={Z} Type={Type} Pending={Pending} Operator={Op}",
            printerId, record.Id, zNumber, record.ClosureType, fiscalPending, operatorName);

        closureDto.ClosureType = fiscalPending ? ClosureType.SoloDatabase : ClosureType.Fiscale;
        closureDto.FiscalClosurePending = fiscalPending;
        closureDto.PrinterErrors = record.PrinterErrors;
        return closureDto;
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
                "Epson ReprintZReportAsync | ClosureId={ClosureId} PrinterId={PrinterId}",
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
            var xml = EpsonXmlBuilder.BuildZReport(closureDto, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            return await ExecuteXmlAsync(channel, xml, record.PrinterId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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

        logger.LogInformation("Epson RetryFiscalClosureAsync | ClosureId={ClosureId} PrinterId={PrinterId}", closureId, record.PrinterId);

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

        try
        {
            await using var channel = await CreateChannelAsync(record.PrinterId, cancellationToken).ConfigureAwait(false);
            var xml = EpsonXmlBuilder.BuildZReport(closureDto, channel.DeviceId, EpsonProtocolConstants.DefaultTimeoutMs);
            var printResult = await ExecuteXmlAsync(channel, xml, record.PrinterId, cancellationToken).ConfigureAwait(false);

            if (printResult.Success)
            {
                record.FiscalClosurePending = false;
                record.ClosureType = "Fiscale";
                record.PrinterErrors = null;
                record.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                closureDto.ClosureType = ClosureType.Fiscale;
                closureDto.FiscalClosurePending = false;
                return closureDto;
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
            logger.LogWarning(ex, "Epson RetryFiscalClosureAsync: printer still unreachable | ClosureId={ClosureId}", closureId);
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
