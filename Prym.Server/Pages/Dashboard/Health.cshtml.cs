using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Prym.Server.Pages.Dashboard;

/// <summary>
/// Health checks monitoring page - displays real-time health status of all registered services.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class HealthModel : PageModel
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthModel> _logger;

    public HealthReport? Report { get; set; }
    public string? ErrorMessage { get; set; }

    public HealthModel(HealthCheckService healthCheckService, ILogger<HealthModel> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Report = await _healthCheckService.CheckHealthAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running health checks");
            ErrorMessage = "Impossibile eseguire i controlli di salute. Verificare i log per dettagli.";
        }
    }
}
