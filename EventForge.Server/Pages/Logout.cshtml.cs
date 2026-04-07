using EventForge.Server.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages;

/// <summary>
/// Logout page — signs out the server cookie authentication session and redirects to login.
/// </summary>
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(AuthenticationSchemes.ServerCookie);
        HttpContext.Session.Clear();
        return RedirectToPage("/ServerAuth/Login");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AuthenticationSchemes.ServerCookie);
        HttpContext.Session.Clear();
        return RedirectToPage("/ServerAuth/Login");
    }
}
