using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Service implementation for application log operations.
/// Provides read-only access to Serilog logs stored in the database.
/// </summary>
public class ApplicationLogService : IApplicationLogService
{
    private readonly EventForgeDbContext _context;

    public ApplicationLogService(EventForgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets a paginated list of application logs with optional filtering and sorting.
    /// </summary>
    public async Task<PagedResult<ApplicationLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var query = BuildLogsQuery(queryParameters);
        var countQuery = BuildCountQuery(queryParameters);

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        // Execute count query for total count
        var totalCount = await connection.QuerySingleAsync<long>(countQuery);

        // Execute main query for data
        var logs = await connection.QueryAsync<ApplicationLogDto>(query);

        return new PagedResult<ApplicationLogDto>
        {
            Items = logs,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gets a specific application log entry by ID.
    /// </summary>
    public async Task<ApplicationLogDto?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE Id = @Id";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var log = await connection.QuerySingleOrDefaultAsync<ApplicationLogDto>(query, new { Id = id });
        return log;
    }

    /// <summary>
    /// Gets application logs filtered by log level.
    /// </summary>
    public async Task<IEnumerable<ApplicationLogDto>> GetLogsByLevelAsync(
        string level,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(level);

        const string query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE Level = @Level
            ORDER BY TimeStamp DESC";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var logs = await connection.QueryAsync<ApplicationLogDto>(query, new { Level = level });
        return logs;
    }

    /// <summary>
    /// Gets application logs within a specific date range.
    /// </summary>
    public async Task<IEnumerable<ApplicationLogDto>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE TimeStamp >= @FromDate AND TimeStamp <= @ToDate
            ORDER BY TimeStamp DESC";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var logs = await connection.QueryAsync<ApplicationLogDto>(query, new { FromDate = fromDate, ToDate = toDate });
        return logs;
    }

    /// <summary>
    /// Gets log statistics grouped by level for a specific date range.
    /// </summary>
    public async Task<Dictionary<string, int>> GetLogStatisticsByLevelAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                Level,
                COUNT(*) as Count
            FROM Logs 
            WHERE TimeStamp >= @FromDate AND TimeStamp <= @ToDate
            GROUP BY Level
            ORDER BY Count DESC";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var results = await connection.QueryAsync<(string Level, int Count)>(query, new { FromDate = fromDate, ToDate = toDate });
        return results.ToDictionary(x => x.Level, x => x.Count);
    }

    /// <summary>
    /// Gets recent error logs (last 24 hours).
    /// </summary>
    public async Task<IEnumerable<ApplicationLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddHours(-24);
        
        const string query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE Level IN ('Error', 'Fatal', 'Critical') 
                AND TimeStamp >= @FromDate
            ORDER BY TimeStamp DESC";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var logs = await connection.QueryAsync<ApplicationLogDto>(query, new { FromDate = fromDate });
        return logs;
    }

    /// <summary>
    /// Gets logs for a specific correlation ID.
    /// </summary>
    public async Task<IEnumerable<ApplicationLogDto>> GetLogsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        const string query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE CorrelationId = @CorrelationId
            ORDER BY TimeStamp ASC";

        using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var logs = await connection.QueryAsync<ApplicationLogDto>(query, new { CorrelationId = correlationId });
        return logs;
    }

    private string BuildLogsQuery(ApplicationLogQueryParameters queryParameters)
    {
        var query = @"
            SELECT 
                Id,
                TimeStamp as Timestamp,
                Level,
                Message,
                Exception,
                Properties,
                LogEvent as Logger,
                MachineName,
                Environment,
                Application,
                CorrelationId,
                UserId,
                RequestPath,
                RequestMethod,
                StatusCode
            FROM Logs 
            WHERE 1=1";

        var conditions = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Level))
            conditions.Add("Level = @Level");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Logger))
            conditions.Add("LogEvent LIKE '%' + @Logger + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Message))
            conditions.Add("Message LIKE '%' + @Message + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.MachineName))
            conditions.Add("MachineName = @MachineName");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Application))
            conditions.Add("Application = @Application");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Environment))
            conditions.Add("Environment = @Environment");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.CorrelationId))
            conditions.Add("CorrelationId = @CorrelationId");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.UserId))
            conditions.Add("UserId = @UserId");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.RequestPath))
            conditions.Add("RequestPath LIKE '%' + @RequestPath + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.RequestMethod))
            conditions.Add("RequestMethod = @RequestMethod");
        
        if (queryParameters.StatusCode.HasValue)
            conditions.Add("StatusCode = @StatusCode");
        
        if (queryParameters.FromDate.HasValue)
            conditions.Add("TimeStamp >= @FromDate");
        
        if (queryParameters.ToDate.HasValue)
            conditions.Add("TimeStamp <= @ToDate");

        if (conditions.Any())
            query += " AND " + string.Join(" AND ", conditions);

        // Add sorting
        var sortDirection = queryParameters.SortDirection.ToLowerInvariant() == "asc" ? "ASC" : "DESC";
        query += $" ORDER BY {queryParameters.SortBy} {sortDirection}";

        // Add pagination
        query += $" OFFSET {queryParameters.Skip} ROWS FETCH NEXT {queryParameters.PageSize} ROWS ONLY";

        return query;
    }

    private string BuildCountQuery(ApplicationLogQueryParameters queryParameters)
    {
        var query = "SELECT COUNT(*) FROM Logs WHERE 1=1";

        var conditions = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Level))
            conditions.Add("Level = @Level");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Logger))
            conditions.Add("LogEvent LIKE '%' + @Logger + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Message))
            conditions.Add("Message LIKE '%' + @Message + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.MachineName))
            conditions.Add("MachineName = @MachineName");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Application))
            conditions.Add("Application = @Application");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.Environment))
            conditions.Add("Environment = @Environment");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.CorrelationId))
            conditions.Add("CorrelationId = @CorrelationId");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.UserId))
            conditions.Add("UserId = @UserId");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.RequestPath))
            conditions.Add("RequestPath LIKE '%' + @RequestPath + '%'");
        
        if (!string.IsNullOrWhiteSpace(queryParameters.RequestMethod))
            conditions.Add("RequestMethod = @RequestMethod");
        
        if (queryParameters.StatusCode.HasValue)
            conditions.Add("StatusCode = @StatusCode");
        
        if (queryParameters.FromDate.HasValue)
            conditions.Add("TimeStamp >= @FromDate");
        
        if (queryParameters.ToDate.HasValue)
            conditions.Add("TimeStamp <= @ToDate");

        if (conditions.Any())
            query += " AND " + string.Join(" AND ", conditions);

        return query;
    }
}