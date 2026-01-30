using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages;

/// <summary>
/// Logout page - clears JWT token and redirects to landing page.
/// </summary>
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        // Return the page which handles client-side token removal
        return Page();
    }

    public IActionResult OnPost()
    {
        // Handle POST request (from form submission)
        // Clear any server-side session if exists
        HttpContext.Session.Clear();

        // Return the page which handles client-side token removal
        return Page();
    }
}
