using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for performance monitoring and statistics.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class PerformanceController : BaseApiController
{
    private readonly IPerformanceMonitoringService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceMonitoringService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets performance statistics including query metrics and slow query information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance statistics</returns>
    /// <response code="200">Returns performance statistics</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user does not have admin privileges</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(PerformanceStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerformanceStatistics>> GetStatistics(CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _performanceService.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance statistics");
            return CreateInternalServerErrorProblem("Unable to retrieve performance statistics", ex);
        }
    }

    /// <summary>
    /// Gets recent slow queries with detailed information.
    /// </summary>
    /// <param name="limit">Maximum number of slow queries to return (default: 50, max: 200)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent slow queries</returns>
    /// <response code="200">Returns list of slow queries</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user does not have admin privileges</response>
    [HttpGet("slow-queries")]
    [ProducesResponseType(typeof(IEnumerable<SlowQueryInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SlowQueryInfo>>> GetSlowQueries(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (limit < 1) limit = 50;
            if (limit > 200) limit = 200;

            var statistics = await _performanceService.GetStatisticsAsync();
            var slowQueries = statistics.RecentSlowQueries
                .OrderByDescending(q => q.Timestamp)
                .Take(limit)
                .ToList();

            return Ok(slowQueries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slow queries");
            return CreateInternalServerErrorProblem("Unable to retrieve slow query information", ex);
        }
    }

    /// <summary>
    /// Gets performance summary with key metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance summary</returns>
    /// <response code="200">Returns performance summary</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user does not have admin privileges</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PerformanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerformanceSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _performanceService.GetStatisticsAsync();

            var summary = new PerformanceSummaryDto
            {
                TotalQueries = statistics.TotalQueries,
                SlowQueries = statistics.SlowQueries,
                SlowQueryPercentage = statistics.SlowQueryPercentage,
                AverageQueryDurationMs = statistics.AverageQueryDuration.TotalMilliseconds,
                SlowestQueryDurationMs = statistics.SlowestQueryDuration.TotalMilliseconds,
                RecentSlowQueryCount = statistics.RecentSlowQueries.Count,
                LastSlowQueryTime = statistics.RecentSlowQueries.LastOrDefault()?.Timestamp,
                PerformanceLevel = GetPerformanceLevel(statistics.SlowQueryPercentage, statistics.AverageQueryDuration)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance summary");
            return CreateInternalServerErrorProblem("Unable to retrieve performance summary", ex);
        }
    }

    private static string GetPerformanceLevel(double slowQueryPercentage, TimeSpan averageQueryDuration)
    {
        if (slowQueryPercentage > 10 || averageQueryDuration.TotalSeconds > 1)
            return "Poor";

        if (slowQueryPercentage > 5 || averageQueryDuration.TotalMilliseconds > 500)
            return "Fair";

        if (slowQueryPercentage > 2 || averageQueryDuration.TotalMilliseconds > 200)
            return "Good";

        return "Excellent";
    }
}

/// <summary>
/// Performance summary DTO for dashboard display.
/// </summary>
public class PerformanceSummaryDto
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
    /// Percentage of slow queries.
    /// </summary>
    public double SlowQueryPercentage { get; set; }

    /// <summary>
    /// Average query duration in milliseconds.
    /// </summary>
    public double AverageQueryDurationMs { get; set; }

    /// <summary>
    /// Slowest query duration in milliseconds.
    /// </summary>
    public double SlowestQueryDurationMs { get; set; }

    /// <summary>
    /// Number of recent slow queries in memory.
    /// </summary>
    public int RecentSlowQueryCount { get; set; }

    /// <summary>
    /// Timestamp of the last slow query.
    /// </summary>
    public DateTime? LastSlowQueryTime { get; set; }

    /// <summary>
    /// Overall performance level (Excellent, Good, Fair, Poor).
    /// </summary>
    public string PerformanceLevel { get; set; } = string.Empty;
}