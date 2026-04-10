using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.CodeGeneration;

/// <summary>
/// Generates unique daily sequential codes using EF Core atomic operations.
/// Format: YYYYMMDDNNNNNN (UTC date + 6-digit zero-padded counter).
/// </summary>
public class DailySequentialCodeGenerator(
    EventForgeDbContext context,
    ILogger<DailySequentialCodeGenerator> logger) : IDailyCodeGenerator
{

    /// <inheritdoc/>
    public async Task<string> GenerateDailyCodeAsync(CancellationToken cancellationToken = default)
    {
        var utcDate = DateTime.UtcNow.Date;
        var dateString = utcDate.ToString("yyyyMMdd");

        var sequenceNumber = await GetNextSequenceNumberAsync(utcDate, cancellationToken);

        var code = $"{dateString}{sequenceNumber:D6}";

        logger.LogDebug("Generated daily code: {Code} for date {Date}", code, utcDate);

        return code;
    }

    private async Task<long> GetNextSequenceNumberAsync(DateTime date, CancellationToken cancellationToken)
    {
        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var sequence = await context.DailySequences
                .FirstOrDefaultAsync(ds => ds.Date == date, cancellationToken);

            if (sequence is null)
            {
                sequence = new EventForge.Server.Data.Entities.DailySequence
                {
                    Date = date,
                    LastNumber = 1
                };
                context.DailySequences.Add(sequence);
            }
            else
            {
                sequence.LastNumber++;
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return sequence.LastNumber;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sequence number for date {Date}", date);
            try { await dbTransaction.RollbackAsync(cancellationToken); } catch { /* best-effort rollback */ }
            throw;
        }
    }

}
