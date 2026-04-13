using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Unified implementation for all log management operations.
/// Consolidates application logs, audit logs, and client logs with performance optimizations.
/// </summary>
public class LogManagementService(
    IApplicationLogService applicationLogService,
    IAuditLogService auditLogService,
    ILogSanitizationService logSanitizationService,
    EventForgeDbContext dbContext,
    ILogger<LogManagementService> logger,
    IMemoryCache cache,
    IConfiguration configuration) : ILogManagementService
{

    private readonly string _logDbConnectionString = configuration.GetConnectionString("LogDb")
            ?? throw new InvalidOperationException("LogDb connection string not found.");
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Cache keys
    private const string CACHE_KEY_LOG_LEVELS = "log_levels";
    private const string CACHE_KEY_LOG_STATS = "log_stats_{0}_{1}";
    private const string CACHE_KEY_MONITORING_CONFIG = "monitoring_config";

    private SqlConnection CreateConnection() => new SqlConnection(_logDbConnectionString);

    #region Application Logs

    public async Task<PagedResult<SystemLogDto>> GetApplicationLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the existing ApplicationLogService with optimizations
            return await applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving application logs with parameters: {@QueryParameters}", queryParameters);
            throw new InvalidOperationException("Failed to retrieve application logs", ex);
        }
    }

    public async Task<SystemLogDto?> GetApplicationLogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await applicationLogService.GetLogByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving application log with ID: {LogId}", id);
            throw new InvalidOperationException($"Failed to retrieve application log with ID {id}", ex);
        }
    }

    public async Task<IEnumerable<SystemLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await applicationLogService.GetRecentErrorLogsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent error logs");
            throw new InvalidOperationException("Failed to retrieve recent error logs", ex);
        }
    }

    public async Task<Dictionary<string, int>> GetLogStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(CACHE_KEY_LOG_STATS, fromDate.ToString("yyyyMMdd"), toDate.ToString("yyyyMMdd"));

        if (cache.TryGetValue(cacheKey, out Dictionary<string, int>? cachedStats) && cachedStats is not null)
        {
            return cachedStats;
        }

        try
        {
            var stats = await applicationLogService.GetLogStatisticsByLevelAsync(fromDate, toDate, cancellationToken);

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Size = 1
            };
            _ = cache.Set(cacheKey, stats, cacheOptions);

            return stats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving log statistics from {FromDate} to {ToDate}", fromDate, toDate);
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
            var adminLogs = await applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);

            // Sanitize the logs for public viewing
            var sanitizedLogs = logSanitizationService.SanitizeLogs(adminLogs.Items).ToList();

            // Return paginated result with sanitized data
            return new PagedResult<SanitizedSystemLogDto>
            {
                Items = sanitizedLogs,
                Page = adminLogs.Page,
                PageSize = adminLogs.PageSize,
                TotalCount = adminLogs.TotalCount
                // TotalPages is calculated property
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving public application logs with parameters: {@QueryParameters}", queryParameters);
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
            logger.LogError(ex, "Failed to process client log: {@ClientLog}", clientLog);
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

                logger.LogWarning(ex, "Failed to process client log at index {Index} in batch", index);
            }
        }

        logger.LogInformation("Processed client log batch: {Total} total, {Success} successful, {Errors} errors",
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
        using var scope = logger.BeginScope(logProperties);

        if (!string.IsNullOrEmpty(clientLog.Exception))
        {
            logger.Log(logLevel, new Exception(clientLog.Exception), "{Message}", clientLog.Message);
        }
        else
        {
            logger.Log(logLevel, "{Message}", clientLog.Message);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Audit Logs (Integration)

    public async Task<PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>> GetAuditLogsAsync(
        Prym.DTOs.SuperAdmin.AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.AuditTrails
                .AsNoTracking()
                .Include(a => a.PerformedByUser)
                .Include(a => a.SourceTenant)
                .Include(a => a.TargetTenant)
                .Include(a => a.TargetUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                query = query.Where(a => (a.Details != null && a.Details.Contains(searchDto.SearchTerm)) ||
                                         (a.IpAddress != null && a.IpAddress.Contains(searchDto.SearchTerm)));
            if (searchDto.UserId.HasValue)
                query = query.Where(a => a.PerformedByUserId == searchDto.UserId.Value);
            if (searchDto.SourceTenantId.HasValue)
                query = query.Where(a => a.SourceTenantId == searchDto.SourceTenantId.Value);
            if (searchDto.TargetTenantId.HasValue)
                query = query.Where(a => a.TargetTenantId == searchDto.TargetTenantId.Value);
            if (searchDto.TargetUserId.HasValue)
                query = query.Where(a => a.TargetUserId == searchDto.TargetUserId.Value);
            if (searchDto.WasSuccessful.HasValue)
                query = query.Where(a => a.WasSuccessful == searchDto.WasSuccessful.Value);
            if (searchDto.CriticalOperation.HasValue)
                query = query.Where(a => a.OperationType >= AuditOperationType.TenantSwitch); // all types are admin-level
            if (searchDto.FromDate.HasValue)
                query = query.Where(a => a.PerformedAt >= searchDto.FromDate.Value);
            if (searchDto.ToDate.HasValue)
                query = query.Where(a => a.PerformedAt <= searchDto.ToDate.Value);
            if (!string.IsNullOrWhiteSpace(searchDto.SessionId))
                query = query.Where(a => a.SessionId == searchDto.SessionId);
            if (!string.IsNullOrWhiteSpace(searchDto.IpAddress))
                query = query.Where(a => a.IpAddress == searchDto.IpAddress);
            if (searchDto.OperationTypes?.Count > 0)
            {
                if (Enum.TryParse<AuditOperationType>(searchDto.OperationTypes[0], out var opType))
                    query = query.Where(a => a.OperationType == opType);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.PerformedAt)
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .Select(a => new Prym.DTOs.SuperAdmin.AuditTrailResponseDto
                {
                    Id = a.Id,
                    OperationType = a.OperationType,
                    Details = a.Details ?? string.Empty,
                    PerformedAt = a.PerformedAt,
                    PerformedByUserId = a.PerformedByUserId,
                    PerformedByUsername = a.PerformedByUser.Username ?? string.Empty,
                    SourceTenantId = a.SourceTenantId,
                    SourceTenantName = a.SourceTenant != null ? a.SourceTenant.Name : null,
                    TargetTenantId = a.TargetTenantId,
                    TargetTenantName = a.TargetTenant != null ? a.TargetTenant.Name : null,
                    TargetUserId = a.TargetUserId,
                    TargetUsername = a.TargetUser != null ? a.TargetUser.Username : null,
                    WasSuccessful = a.WasSuccessful,
                    ErrorMessage = a.ErrorMessage,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    CriticalOperation = true,
                    SessionId = a.SessionId
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving audit logs: {@SearchDto}", searchDto);
            throw new InvalidOperationException("Failed to retrieve audit logs", ex);
        }
    }

    public async Task<Prym.DTOs.SuperAdmin.AuditTrailStatisticsDto> GetAuditStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var all = await dbContext.AuditTrails
                .AsNoTracking()
                .Select(a => new { a.WasSuccessful, a.PerformedAt, a.OperationType })
                .ToListAsync(cancellationToken);

            var byType = all
                .GroupBy(a => a.OperationType.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return new Prym.DTOs.SuperAdmin.AuditTrailStatisticsDto
            {
                TotalOperations = all.Count,
                SuccessfulOperations = all.Count(a => a.WasSuccessful),
                FailedOperations = all.Count(a => !a.WasSuccessful),
                CriticalOperations = all.Count,
                OperationsToday = all.Count(a => a.PerformedAt >= todayStart),
                OperationsThisWeek = all.Count(a => a.PerformedAt >= weekStart),
                OperationsThisMonth = all.Count(a => a.PerformedAt >= monthStart),
                OperationsByType = byType,
                LastUpdated = now
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving audit statistics");
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
                "audit" or "auditlogs" => await auditLogService.ExportAdvancedAsync(exportRequest, cancellationToken),
                "systemlogs" or "applicationlogs" => await applicationLogService.ExportSystemLogsAsync(exportRequest, cancellationToken),
                _ => throw new ArgumentException($"Unsupported export type: {exportRequest.Type}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting logs: {@ExportRequest}", exportRequest);
            throw new InvalidOperationException("Failed to export logs", ex);
        }
    }

    public async Task<LogMonitoringConfigDto> GetMonitoringConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CACHE_KEY_MONITORING_CONFIG, out LogMonitoringConfigDto? cachedConfig) && cachedConfig is not null)
        {
            return cachedConfig;
        }

        try
        {
            var config = await applicationLogService.GetMonitoringConfigAsync(cancellationToken);

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Size = 1
            };
            _ = cache.Set(CACHE_KEY_MONITORING_CONFIG, config, cacheOptions);

            return config;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving monitoring configuration");
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
            var updatedConfig = await applicationLogService.UpdateMonitoringConfigAsync(config, cancellationToken);

            // Clear cache to ensure fresh data
            cache.Remove(CACHE_KEY_MONITORING_CONFIG);

            return updatedConfig;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating monitoring configuration: {@Config}", config);
            throw new InvalidOperationException("Failed to update monitoring configuration", ex);
        }
    }

    public async Task<IEnumerable<string>> GetAvailableLogLevelsAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CACHE_KEY_LOG_LEVELS, out IEnumerable<string>? cachedLevels) && cachedLevels is not null)
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
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Size = 1
            };
            _ = cache.Set(CACHE_KEY_LOG_LEVELS, levelsList, cacheOptions);

            return levelsList;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available log levels");

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
            cache.Remove(CACHE_KEY_LOG_LEVELS);
            cache.Remove(CACHE_KEY_MONITORING_CONFIG);

            // Clear stats cache entries (they have date-based keys)
            // This is a simple approach - in production you might want more sophisticated cache management
            if (cache is MemoryCache memoryCache)
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
                            cache.Remove(key);
                        }
                    }
                }
            }

            logger.LogInformation("Log management cache cleared successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error clearing cache, continuing anyway");
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
            var testCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                Size = 1
            };
            _ = cache.Set(testKey, "test", testCacheOptions);
            var cacheWorks = cache.TryGetValue(testKey, out _);
            cache.Remove(testKey);

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

            logger.LogError(ex, "Log system health check failed");
        }

        return health;
    }

    #endregion

}
