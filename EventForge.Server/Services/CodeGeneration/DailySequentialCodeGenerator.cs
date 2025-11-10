using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EventForge.Server.Services.CodeGeneration;

/// <summary>
/// Generates unique daily sequential codes using SQL Server atomic operations.
/// Format: YYYYMMDDNNNNNN (UTC date + 6-digit zero-padded counter).
/// </summary>
public class DailySequentialCodeGenerator : IDailyCodeGenerator
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<DailySequentialCodeGenerator> _logger;

    public DailySequentialCodeGenerator(
        EventForgeDbContext context,
        ILogger<DailySequentialCodeGenerator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string> GenerateDailyCodeAsync(CancellationToken cancellationToken = default)
    {
        var utcDate = DateTime.UtcNow.Date;
        var dateString = utcDate.ToString("yyyyMMdd");

        // Get the next sequence number atomically using SQL Server locks
        var sequenceNumber = await GetNextSequenceNumberAsync(utcDate, cancellationToken);

        // Format: YYYYMMDDNNNNNN (date + 6-digit zero-padded counter)
        var code = $"{dateString}{sequenceNumber:D6}";

        _logger.LogDebug("Generated daily code: {Code} for date {Date}", code, utcDate);

        return code;
    }

    private async Task<long> GetNextSequenceNumberAsync(DateTime date, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var wasConnectionClosed = connection.State == ConnectionState.Closed;

        try
        {
            if (wasConnectionClosed)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Use raw SQL with UPDLOCK and ROWLOCK for atomic increment
                var sql = @"
                    DECLARE @NextNumber BIGINT;
                    
                    IF EXISTS (SELECT 1 FROM DailySequences WITH (UPDLOCK, ROWLOCK) WHERE Date = @date)
                    BEGIN
                        UPDATE DailySequences 
                        SET LastNumber = LastNumber + 1 
                        WHERE Date = @date;
                        
                        SELECT @NextNumber = LastNumber 
                        FROM DailySequences 
                        WHERE Date = @date;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO DailySequences (Date, LastNumber) 
                        VALUES (@date, 1);
                        
                        SET @NextNumber = 1;
                    END
                    
                    SELECT @NextNumber AS NextNumber;
                ";

                var dateParam = new SqlParameter("@date", SqlDbType.Date) { Value = date };

                var result = await _context.Database
                    .SqlQueryRaw<long>(sql, dateParam)
                    .FirstAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sequence number for date {Date}", date);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            if (wasConnectionClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}
