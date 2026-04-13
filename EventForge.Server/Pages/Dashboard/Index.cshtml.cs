using Prym.DTOs.Dashboard;
using EventForge.Server.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Dashboard overview page - displays live server status and key metrics.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly IServerStatusService _serverStatusService;
    private readonly ILogger<IndexModel> _logger;

    public ServerStatus? Status { get; set; }

    public IndexModel(IServerStatusService serverStatusService, ILogger<IndexModel> logger)
    {
        _serverStatusService = serverStatusService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Status = await _serverStatusService.GetServerStatusAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading server status for dashboard overview");
        }
    }
}
