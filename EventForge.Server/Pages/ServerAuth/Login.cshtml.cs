using EventForge.DTOs.Auth;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EventForge.Server.Pages.ServerAuth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly Services.Auth.IAuthenticationService _authService;
    private readonly ILogger<LoginModel> logger;

    public LoginModel(
        Services.Auth.IAuthenticationService authService,
        ILogger<LoginModel> logger)
    {
        _authService = authService;
        this.logger = logger;
    }

    public void OnGet()
    {
        // Page load - tenant loading handled by JavaScript
    }

    [BindProperty]
    public string TenantCode { get; set; } = string.Empty;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= "/Dashboard/Index";

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Use the existing authentication service
            var loginRequest = new LoginRequestDto
            {
                TenantCode = TenantCode,
                Username = Username,
                Password = Password
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var loginResult = await _authService.LoginAsync(loginRequest, ipAddress, userAgent);

            if (loginResult == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return Page();
            }

            // Create Claims for Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, loginResult.User.Id.ToString()),
                new Claim(ClaimTypes.Name, loginResult.User.Username),
                new Claim(ClaimTypes.Email, loginResult.User.Email),
                new Claim("TenantId", loginResult.User.TenantId.ToString()),
                new Claim("TenantCode", loginResult.Tenant.Code),
            };

            // Add roles
            foreach (var role in loginResult.User.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, "ServerCookie");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            // Sign in with Cookie Authentication
            await HttpContext.SignInAsync("ServerCookie", claimsPrincipal, authProperties);

            logger.LogInformation(
                "User {Username} from tenant {TenantCode} logged in successfully",
                Username, TenantCode);

            return LocalRedirect(returnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed for {Username}", Username);
            ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync("ServerCookie");
        return RedirectToPage("/ServerAuth/Login");
    }
}
