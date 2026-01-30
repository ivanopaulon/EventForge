using EventForge.DTOs.Dashboard;
using EventForge.Server.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly IServerStatusService _statusService;

    public IndexModel(IServerStatusService statusService)
    {
        _statusService = statusService;
    }

    public ServerStatus? Status { get; set; }

    public async Task OnGetAsync()
    {
        Status = await _statusService.GetServerStatusAsync();
    }
}
