using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.ServerAuth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public void OnGet()
    {
        // Page load - no special logic needed
        // Tenant loading and login handled by JavaScript
    }
}
