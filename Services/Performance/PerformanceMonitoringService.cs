using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EventForge.Services.Performance;

/// <summary>
/// Service for monitoring and logging slow database queries.
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Logs a slow query with performance metrics.
    /// </summary>
    /// <param name="query">The SQL query</param>
    /// <param name="duration">Query execution duration</param>
    /// <param name="parameters">Query parameters</param>
    void LogSlowQuery(string query, TimeSpan duration, object? parameters = null);

    /// <summary>
    /// Gets performance statistics.
    /// </summary>
    /// <returns>Performance statistics</returns>
    Task<PerformanceStatistics> GetStatisticsAsync();
}

/// <summary>
/// Performance statistics model.
/// </summary>
public class PerformanceStatistics
{
    /// <summary>
    /// Total number of queries executed.
    /// </summary>
    public long TotalQueries { get; set; }

    /// <summary>
    /// Number of slow queries.
    /// </summary>
    public long SlowQueries { get; set; }

    /// <summary>
    /// Average query duration.
    /// </summary>
    public TimeSpan AverageQueryDuration { get; set; }

    /// <summary>
    /// Slowest query duration.
    /// </summary>
    public TimeSpan SlowestQueryDuration { get; set; }

    /// <summary>
    /// Percentage of slow queries.
    /// </summary>
    public double SlowQueryPercentage => TotalQueries > 0 ? (double)SlowQueries / TotalQueries * 100 : 0;

    /// <summary>
    /// Recent slow queries.
    /// </summary>
    public List<SlowQueryInfo> RecentSlowQueries { get; set; } = new();
}

/// <summary>
/// Information about a slow query.
/// </summary>
public class SlowQueryInfo
{
    /// <summary>
    /// Query timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// SQL query text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Query execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Query parameters (if any).
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// User who executed the query.
    /// </summary>
    public string? User { get; set; }
}

/// <summary>
/// Performance monitoring configuration.
/// </summary>
public class PerformanceMonitoringOptions
{
    /// <summary>
    /// Threshold for slow query logging (default: 2 seconds).
    /// </summary>
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum number of slow queries to keep in memory.
    /// </summary>
    public int MaxSlowQueryHistory { get; set; } = 100;

    /// <summary>
    /// Enable detailed query logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Log all queries (not just slow ones).
    /// </summary>
    public bool LogAllQueries { get; set; } = false;
}

/// <summary>
/// Implementation of performance monitoring service.
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly PerformanceMonitoringOptions _options;
    private readonly List<SlowQueryInfo> _slowQueries = new();
    private long _totalQueries = 0;
    private long _slowQueryCount = 0;
    private TimeSpan _totalDuration = TimeSpan.Zero;
    private TimeSpan _slowestDuration = TimeSpan.Zero;
    private readonly object _lock = new();

    public PerformanceMonitoringService(IConfiguration configuration, ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        _options = configuration.GetSection("Performance:Monitoring").Get<PerformanceMonitoringOptions>() ?? new PerformanceMonitoringOptions();
    }

    public void LogSlowQuery(string query, TimeSpan duration, object? parameters = null)
    {
        lock (_lock)
        {
            _totalQueries++;
            _totalDuration = _totalDuration.Add(duration);

            if (duration > _slowestDuration)
            {
                _slowestDuration = duration;
            }

            bool isSlowQuery = duration >= _options.SlowQueryThreshold;

            if (isSlowQuery)
            {
                _slowQueryCount++;

                var slowQueryInfo = new SlowQueryInfo
                {
                    Timestamp = DateTime.UtcNow,
                    Query = SanitizeQuery(query),
                    Duration = duration,
                    Parameters = parameters?.ToString()
                };

                _slowQueries.Add(slowQueryInfo);

                // Keep only the most recent slow queries
                if (_slowQueries.Count > _options.MaxSlowQueryHistory)
                {
                    _slowQueries.RemoveAt(0);
                }

                // Log slow query
                _logger.LogWarning("Slow query detected: {Duration}ms - {Query}",
                    duration.TotalMilliseconds, SanitizeQuery(query, 200));
            }
            else if (_options.LogAllQueries)
            {
                _logger.LogDebug("Query executed: {Duration}ms - {Query}",
                    duration.TotalMilliseconds, SanitizeQuery(query, 200));
            }
        }
    }

    public Task<PerformanceStatistics> GetStatisticsAsync()
    {
        lock (_lock)
        {
            var averageDuration = _totalQueries > 0
                ? TimeSpan.FromTicks(_totalDuration.Ticks / _totalQueries)
                : TimeSpan.Zero;

            var statistics = new PerformanceStatistics
            {
                TotalQueries = _totalQueries,
                SlowQueries = _slowQueryCount,
                AverageQueryDuration = averageDuration,
                SlowestQueryDuration = _slowestDuration,
                RecentSlowQueries = _slowQueries.ToList()
            };

            return Task.FromResult(statistics);
        }
    }

    private static string SanitizeQuery(string query, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(query))
            return string.Empty;

        // Remove sensitive data patterns
        var sanitized = query
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // Remove multiple spaces
        while (sanitized.Contains("  "))
        {
            sanitized = sanitized.Replace("  ", " ");
        }

        // Truncate if too long
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength) + "...";
        }

        return sanitized.Trim();
    }
}

/// <summary>
/// EF Core interceptor for automatic query performance monitoring.
/// </summary>
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly IPerformanceMonitoringService _performanceService;
    private readonly ILogger<QueryPerformanceInterceptor> _logger;

    public QueryPerformanceInterceptor(IPerformanceMonitoringService performanceService, ILogger<QueryPerformanceInterceptor> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Duration > TimeSpan.Zero)
        {
            try
            {
                _performanceService.LogSlowQuery(
                    command.CommandText,
                    eventData.Duration,
                    command.Parameters.Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging query performance");
            }
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (eventData.Duration > TimeSpan.Zero)
        {
            try
            {
                _performanceService.LogSlowQuery(
                    command.CommandText,
                    eventData.Duration,
                    command.Parameters.Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging query performance");
            }
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Duration > TimeSpan.Zero)
        {
            try
            {
                _performanceService.LogSlowQuery(
                    command.CommandText,
                    eventData.Duration,
                    command.Parameters.Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging query performance");
            }
        }

        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        if (eventData.Duration > TimeSpan.Zero)
        {
            try
            {
                _performanceService.LogSlowQuery(
                    command.CommandText,
                    eventData.Duration,
                    command.Parameters.Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging query performance");
            }
        }

        return base.NonQueryExecuted(command, eventData, result);
    }
}