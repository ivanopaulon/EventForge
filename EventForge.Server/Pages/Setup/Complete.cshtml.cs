using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Setup;

[AllowAnonymous]
public class CompleteModel : PageModel
{
    public string DatabaseName { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;

    public void OnGet()
    {
        DatabaseName = TempData["DatabaseName"]?.ToString() ?? "EventForgeDB";
        AdminUsername = TempData["AdminUsername"]?.ToString() ?? "admin";
    }
}
