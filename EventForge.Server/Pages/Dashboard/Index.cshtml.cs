using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Dashboard overview page - displays high-level server status and metrics.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class IndexModel : PageModel
{
    /// <summary>
    /// Handles GET requests. Currently a placeholder for future dashboard metrics implementation.
    /// </summary>
    public void OnGet()
    {
        // TODO: Add dashboard overview data (metrics, charts, status cards)
    }
}
