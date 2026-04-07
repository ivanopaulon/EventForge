using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Prym.Server.Pages.Dashboard;

/// <summary>
/// Server maintenance page - provides tools for maintenance mode, cache management, and system operations.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class MaintenanceModel : PageModel
{
    private readonly IConfigurationService _configService;
    private readonly IMemoryCache _memoryCache;
    private readonly PrymDbContext _context;
    private readonly ILogger<MaintenanceModel> _logger;

    public bool IsMaintenanceMode { get; set; }
    public long LogCount { get; set; }
    public long AuditCount { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public MaintenanceModel(
        IConfigurationService configService,
        IMemoryCache memoryCache,
        PrymDbContext context,
        ILogger<MaintenanceModel> logger)
    {
        _configService = configService;
        _memoryCache = memoryCache;
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if (TempData["SuccessMessage"] is string ok) SuccessMessage = ok;
        if (TempData["ErrorMessage"] is string err) ErrorMessage = err;

        try
        {
            var modeValue = await _configService.GetValueAsync("System.MaintenanceMode", "false", HttpContext.RequestAborted);
            IsMaintenanceMode = modeValue.Equals("true", StringComparison.OrdinalIgnoreCase);

            LogCount = await _context.SystemOperationLogs.LongCountAsync(HttpContext.RequestAborted);
            AuditCount = await _context.AuditTrails.LongCountAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading maintenance page data");
            ErrorMessage = "Errore nel caricamento dei dati di manutenzione.";
        }
    }

    public async Task<IActionResult> OnPostToggleMaintenanceAsync()
    {
        try
        {
            var current = await _configService.GetValueAsync("System.MaintenanceMode", "false", HttpContext.RequestAborted);
            var newValue = current.Equals("true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
            await _configService.SetValueAsync("System.MaintenanceMode", newValue,
                $"Toggled by {User.Identity?.Name}", HttpContext.RequestAborted);

            var label = newValue == "true" ? "attivata" : "disattivata";
            _logger.LogWarning("Maintenance mode {Mode} by {User}", label, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Modalità manutenzione {label}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling maintenance mode");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostClearMemoryCacheAsync()
    {
        try
        {
            if (_memoryCache is MemoryCache mc)
                mc.Compact(1.0);

            _logger.LogInformation("Memory cache cleared by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Cache in memoria svuotata con successo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing memory cache");
            TempData["ErrorMessage"] = $"Errore durante la pulizia della cache: {ex.Message}";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostForceGcAsync()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            _logger.LogInformation("Forced GC collection triggered by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Garbage collection eseguita. Memoria liberata.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running GC");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPurgeOldLogsAsync(int days = 30)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var deleted = await _context.SystemOperationLogs
                .Where(l => l.CreatedAt < cutoff)
                .ExecuteDeleteAsync(HttpContext.RequestAborted);

            _logger.LogInformation("Purged {Count} log entries older than {Days} days by {User}",
                deleted, days, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Eliminati {deleted} log più vecchi di {days} giorni.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging old logs");
            TempData["ErrorMessage"] = $"Errore durante la pulizia dei log: {ex.Message}";
        }
        return RedirectToPage();
    }
}
