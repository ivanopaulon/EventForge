using Dapper;
using Microsoft.Data.SqlClient;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Service implementation for application log operations.
/// Provides read-only access to Serilog logs stored in the database.
/// </summary>
public class ApplicationLogService : IApplicationLogService
{
    private readonly string _logDbConnectionString;

    public ApplicationLogService(IConfiguration configuration)
    {
        _logDbConnectionString = configuration.GetConnectionString("LogDB")
            ?? throw new InvalidOperationException("LogDB connection string not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_logDbConnectionString);

    /// <summary>
    /// Gets a paginated list of application logs with optional filtering and sorting.
    /// </summary>
    public async Task<PagedResult<SystemLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var query = BuildLogsQuery(queryParameters);
        var countQuery = BuildCountQuery(queryParameters);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var totalCount = await connection.QuerySingleAsync<long>(countQuery, queryParameters);
        var logs = await connection.QueryAsync<SystemLogDto>(query, queryParameters);

        return new PagedResult<SystemLogDto>
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
    public async Task<SystemLogDto?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                Id,
                Message,
                MessageTemplate,
                Level,
                TimeStamp as Timestamp,
                Exception,
                Properties
            FROM Logs 
            WHERE Id = @Id";

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var log = await connection.QuerySingleOrDefaultAsync<SystemLogDto>(query, new { Id = id });
        return log;
    }

    /// <summary>
    /// Gets application logs filtered by log level.
    /// </summary>
    public async Task<IEnumerable<SystemLogDto>> GetLogsByLevelAsync(
        string level,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(level);

        const string query = @"
            SELECT 
                Id,
                Message,
                MessageTemplate,
                Level,
                TimeStamp as Timestamp,
                Exception,
                Properties
            FROM Logs 
            WHERE Level = @Level
            ORDER BY TimeStamp DESC";

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var logs = await connection.QueryAsync<SystemLogDto>(query, new { Level = level });
        return logs;
    }

    /// <summary>
    /// Gets application logs within a specific date range.
    /// </summary>
    public async Task<IEnumerable<SystemLogDto>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                Id,
                Message,
                MessageTemplate,
                Level,
                TimeStamp as Timestamp,
                Exception,
                Properties
            FROM Logs 
            WHERE TimeStamp >= @FromDate AND TimeStamp <= @ToDate
            ORDER BY TimeStamp DESC";

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var logs = await connection.QueryAsync<SystemLogDto>(query, new { FromDate = fromDate, ToDate = toDate });
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

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var results = await connection.QueryAsync<(string Level, int Count)>(query, new { FromDate = fromDate, ToDate = toDate });
        return results.ToDictionary(x => x.Level, x => x.Count);
    }

    /// <summary>
    /// Gets recent error logs (last 24 hours).
    /// </summary>
    public async Task<IEnumerable<SystemLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddHours(-24);

        const string query = @"
            SELECT 
                Id,
                Message,
                MessageTemplate,
                Level,
                TimeStamp as Timestamp,
                Exception,
                Properties
            FROM Logs 
            WHERE Level IN ('Error', 'Fatal', 'Critical') 
                AND TimeStamp >= @FromDate
            ORDER BY TimeStamp DESC";

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var logs = await connection.QueryAsync<SystemLogDto>(query, new { FromDate = fromDate });
        return logs;
    }

    /// <summary>
    /// Gets logs for a specific correlation ID.
    /// </summary>
    public Task<IEnumerable<SystemLogDto>> GetLogsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // La tabella non ha CorrelationId, quindi questa funzione non pu� essere implementata.
        // Si restituisce una lista vuota.
        return Task.FromResult(Enumerable.Empty<SystemLogDto>());
    }

    private string BuildLogsQuery(ApplicationLogQueryParameters queryParameters)
    {
        var query = @"
            SELECT 
                Id,
                Message,
                MessageTemplate,
                Level,
                TimeStamp as Timestamp,
                Exception,
                Properties
            FROM Logs 
            WHERE 1=1";

        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(queryParameters.Level))
            conditions.Add("Level = @Level");

        if (!string.IsNullOrWhiteSpace(queryParameters.Message))
            conditions.Add("Message LIKE '%' + @Message + '%'");

        if (queryParameters.FromDate.HasValue)
            conditions.Add("TimeStamp >= @FromDate");

        if (queryParameters.ToDate.HasValue)
            conditions.Add("TimeStamp <= @ToDate");

        if (conditions.Any())
            query += " AND " + string.Join(" AND ", conditions);

        var sortDirection = queryParameters.SortDirection?.ToLowerInvariant() == "asc" ? "ASC" : "DESC";
        var sortBy = string.IsNullOrWhiteSpace(queryParameters.SortBy) ? "TimeStamp" : queryParameters.SortBy;
        query += $" ORDER BY {sortBy} {sortDirection}";
        query += $" OFFSET {queryParameters.Skip} ROWS FETCH NEXT {queryParameters.PageSize} ROWS ONLY";

        return query;
    }

    private string BuildCountQuery(ApplicationLogQueryParameters queryParameters)
    {
        var query = "SELECT COUNT(*) FROM Logs WHERE 1=1";

        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(queryParameters.Level))
            conditions.Add("Level = @Level");

        if (!string.IsNullOrWhiteSpace(queryParameters.Message))
            conditions.Add("Message LIKE '%' + @Message + '%'");

        if (queryParameters.FromDate.HasValue)
            conditions.Add("TimeStamp >= @FromDate");

        if (queryParameters.ToDate.HasValue)
            conditions.Add("TimeStamp <= @ToDate");

        if (conditions.Any())
            query += " AND " + string.Join(" AND ", conditions);

        return query;
    }

    /// <summary>
    /// Exports system logs with the specified parameters.
    /// </summary>
    public async Task<ExportResultDto> ExportSystemLogsAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportRequest);

        // Validate format
        if (!new[] { "JSON", "CSV", "TXT" }.Contains(exportRequest.Format.ToUpper()))
        {
            throw new ArgumentException("Invalid format. Supported formats: JSON, CSV, TXT");
        }

        // In a real implementation, this would queue the export job for background processing
        await Task.Delay(100, cancellationToken); // Simulate processing

        var exportResult = new ExportResultDto
        {
            Id = Guid.NewGuid(),
            Type = exportRequest.Type,
            Format = exportRequest.Format,
            Status = "Processing",
            RequestedAt = DateTime.UtcNow,
            RequestedBy = "Current User" // Should get from current user context
        };

        return exportResult;
    }

    /// <summary>
    /// Gets the current monitoring configuration.
    /// </summary>
    public async Task<LogMonitoringConfigDto> GetMonitoringConfigAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation

        // In a real implementation, this would be retrieved from configuration storage
        var config = new LogMonitoringConfigDto
        {
            EnableRealTimeUpdates = true,
            UpdateIntervalSeconds = 5,
            MonitoredLevels = new List<string> { "Warning", "Error", "Critical" },
            MonitoredSources = new List<string>(),
            MaxLiveEntries = 100,
            AlertOnCritical = true,
            AlertOnErrors = false
        };

        return config;
    }

    /// <summary>
    /// Updates the monitoring configuration.
    /// </summary>
    public async Task<LogMonitoringConfigDto> UpdateMonitoringConfigAsync(
        LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        await Task.Delay(50, cancellationToken); // Simulate async operation

        // In a real implementation, this would save to configuration storage
        // For now, just return the provided configuration
        return config;
    }
}