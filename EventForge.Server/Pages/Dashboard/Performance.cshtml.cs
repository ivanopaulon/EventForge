using EventForge.DTOs.Dashboard;
using EventForge.Server.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Performance monitoring page - displays server performance metrics and slow queries.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class PerformanceModel : PageModel
{
    private readonly IPerformanceMetricsService _performanceMetricsService;
    private readonly ILogger<PerformanceModel> _logger;

    public PerformanceMetrics? Metrics { get; set; }
    public string? ErrorMessage { get; set; }

    public PerformanceModel(IPerformanceMetricsService performanceMetricsService, ILogger<PerformanceModel> logger)
    {
        _performanceMetricsService = performanceMetricsService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Metrics = await _performanceMetricsService.GetPerformanceMetricsAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading performance metrics");
            ErrorMessage = "Impossibile caricare le metriche di performance.";
        }
    }
}
