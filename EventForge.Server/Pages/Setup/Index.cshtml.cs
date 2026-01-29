using EventForge.Server.Services.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Setup;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IFirstRunDetectionService _firstRunService;
    private readonly ISqlServerDiscoveryService _discoveryService;
    private readonly ISetupWizardService _setupService;

    public IndexModel(
        IFirstRunDetectionService firstRunService,
        ISqlServerDiscoveryService discoveryService,
        ISetupWizardService setupService)
    {
        _firstRunService = firstRunService;
        _discoveryService = discoveryService;
        _setupService = setupService;
    }

    public List<string> DiscoveredSqlServers { get; set; } = new();
    public bool IsKestrel { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await _firstRunService.IsSetupCompleteAsync())
        {
            return RedirectToPage("/Dashboard/Index");
        }

        var instances = await _discoveryService.DiscoverLocalInstancesAsync();
        DiscoveredSqlServers = instances.Select(i => i.InstanceName).ToList();
        IsKestrel = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null;

        return Page();
    }

    [BindProperty]
    public string SqlServerAddress { get; set; } = string.Empty;

    [BindProperty]
    public string AuthType { get; set; } = "windows";

    [BindProperty]
    public string? SqlUsername { get; set; }

    [BindProperty]
    public string? SqlPassword { get; set; }

    [BindProperty]
    public string DatabaseMode { get; set; } = "create";

    [BindProperty]
    public string DatabaseName { get; set; } = "EventForgeDB";

    [BindProperty]
    public int? HttpPort { get; set; }

    [BindProperty]
    public int? HttpsPort { get; set; }

    [BindProperty]
    public bool ForceHttps { get; set; } = true;

    [BindProperty]
    public string JwtSecret { get; set; } = string.Empty;

    [BindProperty]
    public int TokenExpiration { get; set; } = 480;

    [BindProperty]
    public bool EnableRateLimiting { get; set; } = true;

    [BindProperty]
    public bool EnableCors { get; set; }

    [BindProperty]
    public string AdminUsername { get; set; } = "admin";

    [BindProperty]
    public string AdminEmail { get; set; } = string.Empty;

    [BindProperty]
    public string AdminPassword { get; set; } = string.Empty;

    [BindProperty]
    public int LogRetentionDays { get; set; } = 30;

    [BindProperty]
    public bool SeedData { get; set; }

    public async Task<IActionResult> OnPostCompleteSetupAsync()
    {
        if (!ModelState.IsValid)
        {
            var instances = await _discoveryService.DiscoverLocalInstancesAsync();
            DiscoveredSqlServers = instances.Select(i => i.InstanceName).ToList();
            return Page();
        }

        try
        {
            var config = new DTOs.Setup.SetupConfiguration
            {
                ServerAddress = SqlServerAddress,
                Credentials = new DTOs.Setup.SqlCredentials
                {
                    AuthenticationType = AuthType == "windows" ? "Windows" : "SQL",
                    Username = SqlUsername,
                    Password = SqlPassword
                },
                DatabaseName = DatabaseName,
                CreateDatabase = DatabaseMode == "create",
                HttpPort = HttpPort,
                HttpsPort = HttpsPort,
                EnforceHttps = ForceHttps,
                JwtSecretKey = JwtSecret,
                TokenExpirationMinutes = TokenExpiration,
                RateLimitingEnabled = EnableRateLimiting,
                SuperAdminUsername = AdminUsername,
                SuperAdminEmail = AdminEmail,
                SuperAdminPassword = AdminPassword,
                LogRetentionDays = LogRetentionDays,
                SeedDefaultData = SeedData
            };

            var result = await _setupService.CompleteSetupAsync(config);

            if (result.Success)
            {
                return RedirectToPage("/Setup/Complete");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Setup failed");
            var instances = await _discoveryService.DiscoverLocalInstancesAsync();
            DiscoveredSqlServers = instances.Select(i => i.InstanceName).ToList();
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Setup failed: {ex.Message}");
            var instances = await _discoveryService.DiscoverLocalInstancesAsync();
            DiscoveredSqlServers = instances.Select(i => i.InstanceName).ToList();
            return Page();
        }
    }
}
