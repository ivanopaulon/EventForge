using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Internal POCO class representing a log record as stored in the database.
/// Used for Dapper mapping to avoid casting issues between int and Guid.
/// </summary>
internal class DbLogRecord
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageTemplate { get; set; }
    public string Level { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
}

/// <summary>
/// Service implementation for application log operations.
/// Provides read-only access to Serilog logs stored in the database.
/// </summary>
public class ApplicationLogService(
    IConfiguration configuration,
    EventForgeDbContext dbContext,
    IMemoryCache memoryCache,
    ILogger<ApplicationLogService> logger) : IApplicationLogService
{
    private readonly string _logDbConnectionString = configuration.GetConnectionString("LogDb")
            ?? throw new InvalidOperationException("LogDb connection string not found.");
    private readonly ILogger<ApplicationLogService> _logger = logger;
    private const string MonitoringConfigCacheKey = "app_log_monitoring_config";
    private static string LogExportCacheKey(Guid id) => $"log_export_{id}";

    private SqlConnection CreateConnection() => new SqlConnection(_logDbConnectionString);

    /// <summary>
    /// Maps a database log record to a SystemLogDto with deterministic Guid generation.
    /// </summary>
    private static SystemLogDto MapDbRecordToSystemLogDto(DbLogRecord record)
    {
        // Generate deterministic Guid from int Id
        var guidBytes = new byte[16];
        var idBytes = BitConverter.GetBytes(record.Id);
        Array.Copy(idBytes, 0, guidBytes, 0, 4);
        var deterministicGuid = new Guid(guidBytes);

        // Parse Properties JSON if present
        Dictionary<string, object>? properties = null;
        if (!string.IsNullOrWhiteSpace(record.Properties))
        {
            try
            {
                properties = JsonSerializer.Deserialize<Dictionary<string, object>>(record.Properties);
            }
            catch (JsonException)
            {
                // If JSON parsing fails, leave properties as null
                properties = null;
            }
        }

        return new SystemLogDto
        {
            Id = deterministicGuid,
            Timestamp = record.Timestamp,
            Level = record.Level,
            Message = record.Message,
            Exception = record.Exception,
            Properties = properties
        };
    }

    /// <summary>
    /// Gets a paginated list of application logs with optional filtering and sorting.
    /// </summary>
    public async Task<PagedResult<SystemLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        try
        {
            var query = BuildLogsQuery(queryParameters);
            var countQuery = BuildCountQuery(queryParameters);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var totalCount = await connection.QuerySingleAsync<long>(countQuery, queryParameters);
            var dbRecords = await connection.QueryAsync<DbLogRecord>(query, queryParameters);

            // Map database records to DTOs
            var logs = dbRecords.Select(MapDbRecordToSystemLogDto);

            return new PagedResult<SystemLogDto>
            {
                Items = logs,
                Page = queryParameters.Page,
                PageSize = queryParameters.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPagedLogsAsync for page {Page}, pageSize {PageSize}.", queryParameters.Page, queryParameters.PageSize);
            throw;
        }
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

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var dbRecord = await connection.QuerySingleOrDefaultAsync<DbLogRecord>(query, new { Id = id });
            return dbRecord is not null ? MapDbRecordToSystemLogDto(dbRecord) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLogByIdAsync for log {Id}.", id);
            throw;
        }
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

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var dbRecords = await connection.QueryAsync<DbLogRecord>(query, new { Level = level });
            return dbRecords.Select(MapDbRecordToSystemLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLogsByLevelAsync for level {Level}.", level);
            throw;
        }
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

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var dbRecords = await connection.QueryAsync<DbLogRecord>(query, new { FromDate = fromDate, ToDate = toDate });
            return dbRecords.Select(MapDbRecordToSystemLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLogsInDateRangeAsync from {FromDate} to {ToDate}.", fromDate, toDate);
            throw;
        }
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

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<(string Level, int Count)>(query, new { FromDate = fromDate, ToDate = toDate });
            return results.ToDictionary(x => x.Level, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLogStatisticsByLevelAsync from {FromDate} to {ToDate}.", fromDate, toDate);
            throw;
        }
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

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var dbRecords = await connection.QueryAsync<DbLogRecord>(query, new { FromDate = fromDate });
            return dbRecords.Select(MapDbRecordToSystemLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRecentErrorLogsAsync.");
            throw;
        }
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

        AppendSourceCondition(conditions, queryParameters.Source);

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

        AppendSourceCondition(conditions, queryParameters.Source);

        if (conditions.Any())
            query += " AND " + string.Join(" AND ", conditions);

        return query;
    }

    /// <summary>
    /// Appends a WHERE condition to <paramref name="conditions"/> that filters log rows by
    /// their originating component, identified by the well-known message prefix written by
    /// each log forwarding pipeline:
    /// <list type="bullet">
    ///   <item><description><c>Client</c> → Message starts with <c>[Client]</c></description></item>
    ///   <item><description><c>Agent</c>  → Message starts with <c>[Agent:</c></description></item>
    ///   <item><description><c>Server</c> → all other rows (no recognised prefix)</description></item>
    /// </list>
    /// </summary>
    private static void AppendSourceCondition(List<string> conditions, string? source)
    {
        switch (source?.ToLowerInvariant())
        {
            case "client":
                conditions.Add("Message LIKE '[Client] %'");
                break;
            case "agent":
                conditions.Add("Message LIKE '[Agent:%'");
                break;
            case "server":
                conditions.Add("Message NOT LIKE '[Client] %' AND Message NOT LIKE '[Agent:%'");
                break;
        }
    }

    /// <summary>
    /// Exports system logs in JSON or CSV format and caches the result for 24h.
    /// </summary>
    public async Task<ExportResultDto> ExportSystemLogsAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportRequest);

        var format = exportRequest.Format.ToUpperInvariant();
        if (!new[] { "JSON", "CSV", "TXT" }.Contains(format))
            throw new ArgumentException("Invalid format. Supported formats: JSON, CSV, TXT");

        try
        {
            var maxRecords = exportRequest.MaxRecords ?? 10_000;

            var whereClause = BuildExportWhereClause(exportRequest, out var parameters);
            var sql = $@"
                SELECT TOP (@MaxRecords) Id, Message, MessageTemplate, Level, Timestamp, Exception, Properties
                FROM Logs
                {whereClause}
                ORDER BY Timestamp DESC";

            parameters.Add("MaxRecords", maxRecords);

            IEnumerable<DbLogRecord> dbRecords;
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync(cancellationToken);
                dbRecords = await connection.QueryAsync<DbLogRecord>(sql, parameters);
            }

            var records = dbRecords.ToList();

            byte[] bytes = format == "CSV" ? BuildLogCsvExport(records) : BuildLogJsonExport(records, format);

            var exportId = Guid.NewGuid();
            var fileName = exportRequest.FileName ?? $"logs_export_{exportId:N}.{format.ToLowerInvariant()}";
            var contentType = format == "CSV" ? "text/csv" : "application/json";

            memoryCache.Set(
                LogExportCacheKey(exportId),
                (bytes, format, fileName, contentType),
                absoluteExpirationRelativeToNow: TimeSpan.FromHours(24));

            var result = new ExportResultDto
            {
                Id = exportId,
                Type = exportRequest.Type,
                Format = format,
                Status = "Completed",
                TotalRecords = records.Count,
                FileName = fileName,
                DownloadUrl = $"/api/v1/logs/export/{exportId}/download",
                FileSizeBytes = bytes.Length,
                RequestedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                RequestedBy = "System",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExportSystemLogsAsync for type {Type}, format {Format}.", exportRequest.Type, exportRequest.Format);
            throw;
        }
    }

    private static string BuildExportWhereClause(ExportRequestDto req, out Dictionary<string, object> parameters)
    {
        parameters = new Dictionary<string, object>();
        var conditions = new List<string>();
        if (req.FromDate.HasValue) { conditions.Add("Timestamp >= @From"); parameters["From"] = req.FromDate.Value; }
        if (req.ToDate.HasValue) { conditions.Add("Timestamp <= @To"); parameters["To"] = req.ToDate.Value; }
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }

    private static byte[] BuildLogJsonExport(IReadOnlyList<DbLogRecord> records, string format)
    {
        var payload = new { generatedAt = DateTime.UtcNow, format, recordCount = records.Count, records };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private static byte[] BuildLogCsvExport(IReadOnlyList<DbLogRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Level,Timestamp,Message,Exception");
        foreach (var r in records)
        {
            sb.AppendLine(string.Join(",",
                r.Id.ToString(),
                LogCsvEscape(r.Level),
                r.Timestamp.ToString("O"),
                LogCsvEscape(r.Message),
                LogCsvEscape(r.Exception)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string LogCsvEscape(string? v)
    {
        if (v is null) return string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    /// <summary>
    /// Gets the monitoring configuration from SystemConfigurations (key prefix "Log.Monitor.").
    /// </summary>
    public async Task<LogMonitoringConfigDto> GetMonitoringConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (memoryCache.TryGetValue(MonitoringConfigCacheKey, out LogMonitoringConfigDto? cached) && cached is not null)
                return cached;

            var keys = await dbContext.SystemConfigurations
                .AsNoTracking()
                .Where(c => c.Key.StartsWith("Log.Monitor."))
                .ToDictionaryAsync(c => c.Key, c => c.Value, cancellationToken);

            var config = new LogMonitoringConfigDto
            {
                EnableRealTimeUpdates = keys.TryGetValue("Log.Monitor.EnableRealTimeUpdates", out var v1) ? bool.TryParse(v1, out var b1) && b1 : true,
                UpdateIntervalSeconds = keys.TryGetValue("Log.Monitor.UpdateIntervalSeconds", out var v2) && int.TryParse(v2, out var i2) ? i2 : 5,
                MonitoredLevels = keys.TryGetValue("Log.Monitor.MonitoredLevels", out var v3) && !string.IsNullOrWhiteSpace(v3)
                    ? [.. v3.Split(',', StringSplitOptions.RemoveEmptyEntries)]
                    : ["Warning", "Error", "Critical"],
                MonitoredSources = keys.TryGetValue("Log.Monitor.MonitoredSources", out var v4) && !string.IsNullOrWhiteSpace(v4)
                    ? [.. v4.Split(',', StringSplitOptions.RemoveEmptyEntries)]
                    : [],
                MaxLiveEntries = keys.TryGetValue("Log.Monitor.MaxLiveEntries", out var v5) && int.TryParse(v5, out var i5) ? i5 : 100,
                AlertOnCritical = keys.TryGetValue("Log.Monitor.AlertOnCritical", out var v6) ? bool.TryParse(v6, out var b6) && b6 : true,
                AlertOnErrors = keys.TryGetValue("Log.Monitor.AlertOnErrors", out var v7) && bool.TryParse(v7, out var b7) && b7
            };

            memoryCache.Set(MonitoringConfigCacheKey, config, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5));
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMonitoringConfigAsync.");
            throw;
        }
    }

    /// <summary>
    /// Persists the monitoring configuration to SystemConfigurations and refreshes the cache.
    /// </summary>
    public async Task<LogMonitoringConfigDto> UpdateMonitoringConfigAsync(
        LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var keyValues = new Dictionary<string, string>
            {
                ["Log.Monitor.EnableRealTimeUpdates"] = config.EnableRealTimeUpdates.ToString(),
                ["Log.Monitor.UpdateIntervalSeconds"] = config.UpdateIntervalSeconds.ToString(),
                ["Log.Monitor.MonitoredLevels"] = string.Join(",", config.MonitoredLevels),
                ["Log.Monitor.MonitoredSources"] = string.Join(",", config.MonitoredSources),
                ["Log.Monitor.MaxLiveEntries"] = config.MaxLiveEntries.ToString(),
                ["Log.Monitor.AlertOnCritical"] = config.AlertOnCritical.ToString(),
                ["Log.Monitor.AlertOnErrors"] = config.AlertOnErrors.ToString()
            };

            foreach (var kv in keyValues)
            {
                var existing = await dbContext.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == kv.Key, cancellationToken);

                if (existing is null)
                {
                    dbContext.SystemConfigurations.Add(new Data.Entities.Configuration.SystemConfiguration
                    {
                        Key = kv.Key,
                        Value = kv.Value,
                        Category = "Logging"
                    });
                }
                else
                {
                    existing.Value = kv.Value;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            memoryCache.Remove(MonitoringConfigCacheKey);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMonitoringConfigAsync.");
            throw;
        }
    }

}
