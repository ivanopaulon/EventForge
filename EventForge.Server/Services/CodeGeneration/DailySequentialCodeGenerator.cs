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

            // Use a plain ADO.NET transaction on the open connection so we can execute
            // the multi-statement SQL atomically without EF Core trying to compose over it.
            using var dbTransaction = connection.BeginTransaction();

            try
            {
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

                using var cmd = connection.CreateCommand();
                cmd.Transaction = dbTransaction;
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                var param = cmd.CreateParameter();
                param.ParameterName = "@date";
                param.DbType = DbType.Date;
                param.Value = date;
                cmd.Parameters.Add(param);

                var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

                if (scalar == null || scalar == DBNull.Value)
                {
                    throw new InvalidOperationException("Sequence query did not return a value.");
                }

                var result = Convert.ToInt64(scalar);

                // Commit the ADO.NET transaction
                dbTransaction.Commit();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sequence number for date {Date}", date);
                try
                {
                    dbTransaction.Rollback();
                }
                catch
                {
                    // Best-effort rollback; swallow to preserve original exception
                }
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
