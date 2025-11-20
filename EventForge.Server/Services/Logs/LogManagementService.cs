using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Unified implementation for all log management operations.
/// Consolidates application logs, audit logs, and client logs with performance optimizations.
/// </summary>
public class LogManagementService : ILogManagementService
{
    private readonly IApplicationLogService _applicationLogService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogSanitizationService _logSanitizationService;
    private readonly ILogger<LogManagementService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _logDbConnectionString;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Cache keys
    private const string CACHE_KEY_LOG_LEVELS = "log_levels";
    private const string CACHE_KEY_LOG_STATS = "log_stats_{0}_{1}";
    private const string CACHE_KEY_MONITORING_CONFIG = "monitoring_config";

    public LogManagementService(
        IApplicationLogService applicationLogService,
        IAuditLogService auditLogService,
        ILogSanitizationService logSanitizationService,
        ILogger<LogManagementService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _applicationLogService = applicationLogService ?? throw new ArgumentNullException(nameof(applicationLogService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logSanitizationService = logSanitizationService ?? throw new ArgumentNullException(nameof(logSanitizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _logDbConnectionString = configuration.GetConnectionString("LogDb")
            ?? throw new InvalidOperationException("LogDb connection string not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_logDbConnectionString);

    #region Application Logs

    public async Task<PagedResult<SystemLogDto>> GetApplicationLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the existing ApplicationLogService with optimizations
            return await _applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application logs with parameters: {@QueryParameters}", queryParameters);
            throw new InvalidOperationException("Failed to retrieve application logs", ex);
        }
    }

    public async Task<SystemLogDto?> GetApplicationLogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _applicationLogService.GetLogByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application log with ID: {LogId}", id);
            throw new InvalidOperationException($"Failed to retrieve application log with ID {id}", ex);
        }
    }

    public async Task<IEnumerable<SystemLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _applicationLogService.GetRecentErrorLogsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent error logs");
            throw new InvalidOperationException("Failed to retrieve recent error logs", ex);
        }
    }

    public async Task<Dictionary<string, int>> GetLogStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(CACHE_KEY_LOG_STATS, fromDate.ToString("yyyyMMdd"), toDate.ToString("yyyyMMdd"));

        if (_cache.TryGetValue(cacheKey, out Dictionary<string, int>? cachedStats) && cachedStats != null)
        {
            return cachedStats;
        }

        try
        {
            var stats = await _applicationLogService.GetLogStatisticsByLevelAsync(fromDate, toDate, cancellationToken);

            // Cache for 5 minutes
            _ = _cache.Set(cacheKey, stats, _cacheExpiration);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log statistics from {FromDate} to {ToDate}", fromDate, toDate);
            throw new InvalidOperationException("Failed to retrieve log statistics", ex);
        }
    }

    public async Task<PagedResult<SanitizedSystemLogDto>> GetPublicApplicationLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get full logs using admin service
            var adminLogs = await _applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);

            // Sanitize the logs for public viewing
            var sanitizedLogs = _logSanitizationService.SanitizeLogs(adminLogs.Items).ToList();

            // Return paginated result with sanitized data
            return new PagedResult<SanitizedSystemLogDto>
            {
                Items = sanitizedLogs,
                Page = adminLogs.Page,
                PageSize = adminLogs.PageSize,
                TotalCount = adminLogs.TotalCount,
                TotalPages = adminLogs.TotalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public application logs with parameters: {@QueryParameters}", queryParameters);
            throw new InvalidOperationException("Failed to retrieve public application logs", ex);
        }
    }

    #endregion

    #region Client Logs

    public async Task ProcessClientLogAsync(ClientLogDto clientLog, string? userContext = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientLog);

        try
        {
            // Enhance the client log with server-side information
            var enrichedLog = EnrichClientLog(clientLog, userContext);

            // Log to the unified Serilog infrastructure
            await LogClientLogToSerilog(enrichedLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log: {@ClientLog}", clientLog);
            throw new InvalidOperationException("Failed to process client log", ex);
        }
    }

    public async Task<BatchProcessingResult> ProcessClientLogBatchAsync(
        IEnumerable<ClientLogDto> clientLogs,
        string? userContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientLogs);

        var result = new BatchProcessingResult();
        var logList = clientLogs.ToList();
        result.TotalCount = logList.Count;

        foreach (var (log, index) in logList.Select((log, index) => (log, index)))
        {
            try
            {
                await ProcessClientLogAsync(log, userContext, cancellationToken);
                result.SuccessCount++;
                result.Results.Add(new BatchItemResult
                {
                    Index = index,
                    Status = "success"
                });
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Results.Add(new BatchItemResult
                {
                    Index = index,
                    Status = "error",
                    ErrorMessage = ex.Message
                });

                _logger.LogWarning(ex, "Failed to process client log at index {Index} in batch", index);
            }
        }

        _logger.LogInformation("Processed client log batch: {Total} total, {Success} successful, {Errors} errors",
            result.TotalCount, result.SuccessCount, result.ErrorCount);

        return result;
    }

    private ClientLogDto EnrichClientLog(ClientLogDto clientLog, string? userContext)
    {
        // Create a copy to avoid modifying the original
        var enriched = new ClientLogDto
        {
            Level = clientLog.Level,
            Message = clientLog.Message,
            Page = clientLog.Page,
            UserId = clientLog.UserId,
            Exception = clientLog.Exception,
            UserAgent = clientLog.UserAgent,
            Properties = clientLog.Properties,
            Timestamp = clientLog.Timestamp,
            CorrelationId = clientLog.CorrelationId ?? Guid.NewGuid().ToString(),
            Category = clientLog.Category ?? "ClientLog"
        };

        return enriched;
    }

    private async Task LogClientLogToSerilog(ClientLogDto clientLog)
    {
        // Map client log level to Serilog LogLevel
        var logLevel = clientLog.Level.ToLowerInvariant() switch
        {
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        // Create structured log entry with all client log properties
        var logProperties = new Dictionary<string, object>
        {
            ["Source"] = "Client",
            ["UserId"] = clientLog.UserId?.ToString() ?? "anonymous",
            ["Page"] = clientLog.Page ?? "unknown",
            ["ClientTimestamp"] = clientLog.Timestamp,
            ["CorrelationId"] = clientLog.CorrelationId ?? string.Empty,
            ["UserAgent"] = clientLog.UserAgent ?? string.Empty,
            ["Category"] = clientLog.Category ?? "ClientLog",
            ["ClientProperties"] = clientLog.Properties ?? string.Empty
        };

        // Log using structured logging
        using var scope = _logger.BeginScope(logProperties);

        if (!string.IsNullOrEmpty(clientLog.Exception))
        {
            _logger.Log(logLevel, new Exception(clientLog.Exception), "{Message}", clientLog.Message);
        }
        else
        {
            _logger.Log(logLevel, "{Message}", clientLog.Message);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Audit Logs (Integration)

    public async Task<PagedResult<AuditTrailResponseDto>> GetAuditLogsAsync(
        AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditLogService.SearchAuditTrailAsync(searchDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs: {@SearchDto}", searchDto);
            throw new InvalidOperationException("Failed to retrieve audit logs", ex);
        }
    }

    public async Task<AuditTrailStatisticsDto> GetAuditStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditLogService.GetAuditTrailStatisticsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            throw new InvalidOperationException("Failed to retrieve audit statistics", ex);
        }
    }

    #endregion

    #region Export and Monitoring

    public async Task<ExportResultDto> ExportLogsAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportRequest);

        // Validate Type parameter
        var validTypes = new[] { "audit", "auditlogs", "systemlogs", "applicationlogs" };
        if (string.IsNullOrWhiteSpace(exportRequest.Type) ||
            !validTypes.Contains(exportRequest.Type.ToLowerInvariant()))
        {
            throw new ArgumentException($"Invalid export type '{exportRequest.Type}'. Supported types: {string.Join(", ", validTypes)}");
        }

        // Validate Format parameter
        var validFormats = new[] { "json", "csv", "excel", "txt" };
        if (string.IsNullOrWhiteSpace(exportRequest.Format) ||
            !validFormats.Contains(exportRequest.Format.ToLowerInvariant()))
        {
            throw new ArgumentException($"Invalid export format '{exportRequest.Format}'. Supported formats: {string.Join(", ", validFormats)}");
        }

        try
        {
            // Delegate to appropriate service based on export type
            return exportRequest.Type.ToLowerInvariant() switch
            {
                "audit" or "auditlogs" => await _auditLogService.ExportAdvancedAsync(exportRequest, cancellationToken),
                "systemlogs" or "applicationlogs" => await _applicationLogService.ExportSystemLogsAsync(exportRequest, cancellationToken),
                _ => throw new ArgumentException($"Unsupported export type: {exportRequest.Type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs: {@ExportRequest}", exportRequest);
            throw new InvalidOperationException("Failed to export logs", ex);
        }
    }

    public async Task<LogMonitoringConfigDto> GetMonitoringConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CACHE_KEY_MONITORING_CONFIG, out LogMonitoringConfigDto? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        try
        {
            var config = await _applicationLogService.GetMonitoringConfigAsync(cancellationToken);

            // Cache for 5 minutes
            _ = _cache.Set(CACHE_KEY_MONITORING_CONFIG, config, _cacheExpiration);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring configuration");
            throw new InvalidOperationException("Failed to retrieve monitoring configuration", ex);
        }
    }

    public async Task<LogMonitoringConfigDto> UpdateMonitoringConfigurationAsync(
        LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var updatedConfig = await _applicationLogService.UpdateMonitoringConfigAsync(config, cancellationToken);

            // Clear cache to ensure fresh data
            _cache.Remove(CACHE_KEY_MONITORING_CONFIG);

            return updatedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating monitoring configuration: {@Config}", config);
            throw new InvalidOperationException("Failed to update monitoring configuration", ex);
        }
    }

    public async Task<IEnumerable<string>> GetAvailableLogLevelsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CACHE_KEY_LOG_LEVELS, out IEnumerable<string>? cachedLevels) && cachedLevels != null)
        {
            return cachedLevels;
        }

        try
        {
            const string query = @"
                SELECT DISTINCT Level 
                FROM Logs 
                WHERE Level IS NOT NULL 
                ORDER BY Level";

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var levels = await connection.QueryAsync<string>(query);
            var levelsList = levels.ToList();

            // Cache for 10 minutes since log levels don't change frequently
            _ = _cache.Set(CACHE_KEY_LOG_LEVELS, levelsList, TimeSpan.FromMinutes(10));

            return levelsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available log levels");

            // Return default levels if database query fails
            return new[] { "Debug", "Information", "Warning", "Error", "Critical" };
        }
    }

    #endregion

    #region Performance and Maintenance

    public async Task ClearCacheAsync()
    {
        try
        {
            _cache.Remove(CACHE_KEY_LOG_LEVELS);
            _cache.Remove(CACHE_KEY_MONITORING_CONFIG);

            // Clear stats cache entries (they have date-based keys)
            // This is a simple approach - in production you might want more sophisticated cache management
            if (_cache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (entriesCollection?.GetValue(coherentState) is System.Collections.IDictionary entries)
                    {
                        var keysToRemove = new List<object>();
                        foreach (System.Collections.DictionaryEntry entry in entries)
                        {
                            if (entry.Key.ToString()?.StartsWith("log_stats_") == true)
                            {
                                keysToRemove.Add(entry.Key);
                            }
                        }

                        foreach (var key in keysToRemove)
                        {
                            _cache.Remove(key);
                        }
                    }
                }
            }

            _logger.LogInformation("Log management cache cleared successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cache, continuing anyway");
        }
    }

    public async Task<LogSystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var health = new LogSystemHealthDto
        {
            IsHealthy = true,
            Status = "healthy"
        };

        try
        {
            // Test database connectivity
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var logCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Logs WHERE TimeStamp >= @Since",
                new { Since = DateTime.UtcNow.AddHours(-1) });

            health.Details["RecentLogCount"] = logCount;
            health.Details["DatabaseConnectivity"] = "OK";

            // Test cache functionality
            var testKey = $"health_test_{Guid.NewGuid()}";
            _ = _cache.Set(testKey, "test", TimeSpan.FromMinutes(1));
            var cacheWorks = _cache.TryGetValue(testKey, out _);
            _cache.Remove(testKey);

            health.Details["CacheConnectivity"] = cacheWorks ? "OK" : "FAILED";

            if (!cacheWorks)
            {
                health.IsHealthy = false;
                health.Status = "degraded";
            }
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.Status = "unhealthy";
            health.Details["Error"] = ex.Message;

            _logger.LogError(ex, "Log system health check failed");
        }

        return health;
    }

    #endregion
}