using EventForge.Server.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class MaintenanceModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;

    public MaintenanceModel(
        EventForgeDbContext context,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime)
    {
        _context = context;
        _configuration = configuration;
        _lifetime = lifetime;
    }

    public bool IsMaintenanceMode { get; set; }
    public string MaintenanceMessage { get; set; } = string.Empty;
    public int LogRetentionDays { get; set; }
    public int TotalLogEntries { get; set; }
    public DateTime? OldestLogEntry { get; set; }
    public DateTime? NextCleanup { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public int DatabaseSizeMb { get; set; }
    public int TableCount { get; set; }

    public async Task OnGetAsync()
    {
        // Load configuration values from key-value store
        var configs = await _context.Set<SystemConfiguration>().ToListAsync();
        var configDict = configs.ToDictionary(c => c.Key, c => c.Value);

        IsMaintenanceMode = configDict.TryGetValue("MaintenanceMode", out var mm) && mm == "true";
        MaintenanceMessage = configDict.TryGetValue("MaintenanceMessage", out var msg) ? msg : string.Empty;
        LogRetentionDays = configDict.TryGetValue("LogRetentionDays", out var lrd) && int.TryParse(lrd, out var days) ? days : 30;

        var nextCleanup = configDict.TryGetValue("NextLogCleanup", out var nlc) ? nlc : null;
        if (!string.IsNullOrEmpty(nextCleanup) && DateTime.TryParse(nextCleanup, out var dt))
        {
            NextCleanup = dt;
        }

        TotalLogEntries = await _context.Set<LogEntry>().CountAsync();
        OldestLogEntry = await _context.Set<LogEntry>()
            .OrderBy(l => l.TimeStamp)
            .Select(l => l.TimeStamp)
            .FirstOrDefaultAsync();

        DatabaseName = _context.Database.GetDbConnection().Database;
        TableCount = await GetTableCountAsync();
        DatabaseSizeMb = await GetDatabaseSizeAsync();
    }

    public async Task<IActionResult> OnPostToggleMaintenanceAsync(bool enabled, string? message)
    {
        var mmConfig = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Key == "MaintenanceMode");

        if (mmConfig != null)
        {
            mmConfig.Value = enabled.ToString().ToLower();
        }
        else
        {
            _context.Set<SystemConfiguration>().Add(new SystemConfiguration
            {
                Key = "MaintenanceMode",
                Value = enabled.ToString().ToLower(),
                Category = "System"
            });
        }

        var msgConfig = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Key == "MaintenanceMessage");

        if (msgConfig != null)
        {
            msgConfig.Value = message ?? string.Empty;
        }
        else
        {
            _context.Set<SystemConfiguration>().Add(new SystemConfiguration
            {
                Key = "MaintenanceMessage",
                Value = message ?? string.Empty,
                Category = "System"
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Maintenance mode settings updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateLogRetentionAsync(int retentionDays)
    {
        if (retentionDays < 7 || retentionDays > 365)
        {
            TempData["ErrorMessage"] = "Retention days must be between 7 and 365.";
            return RedirectToPage();
        }

        var lrConfig = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Key == "LogRetentionDays");

        if (lrConfig != null)
        {
            lrConfig.Value = retentionDays.ToString();
        }
        else
        {
            _context.Set<SystemConfiguration>().Add(new SystemConfiguration
            {
                Key = "LogRetentionDays",
                Value = retentionDays.ToString(),
                Category = "Logging"
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Log retention policy updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCleanupLogsAsync()
    {
        var lrConfig = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Key == "LogRetentionDays");

        var retentionDays = 30;
        if (lrConfig != null && int.TryParse(lrConfig.Value, out var days))
        {
            retentionDays = days;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedCount = await _context.Set<LogEntry>()
            .Where(l => l.TimeStamp < cutoffDate)
            .ExecuteDeleteAsync();

        var ncConfig = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Key == "NextLogCleanup");

        var nextCleanup = DateTime.UtcNow.AddDays(1).ToString("o");
        if (ncConfig != null)
        {
            ncConfig.Value = nextCleanup;
        }
        else
        {
            _context.Set<SystemConfiguration>().Add(new SystemConfiguration
            {
                Key = "NextLogCleanup",
                Value = nextCleanup,
                Category = "Logging"
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cleanup completed. {deletedCount} log entries deleted.";
        return RedirectToPage();
    }

    public IActionResult OnPostRestartServer()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            _lifetime.StopApplication();
        });

        TempData["SuccessMessage"] = "Server restart initiated. The application will shut down in a moment.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOptimizeDatabaseAsync()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DBCC UPDATEUSAGE(0) WITH NO_INFOMSGS");

            TempData["SuccessMessage"] = "Database optimization completed successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Database optimization failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task<int> GetTableCountAsync()
    {
        try
        {
            var result = await _context.Database
                .SqlQuery<int>($"SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
                .FirstOrDefaultAsync();
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetDatabaseSizeAsync()
    {
        try
        {
            var result = await _context.Database
                .SqlQuery<int>($"SELECT SUM(size) * 8 / 1024 as Value FROM sys.database_files")
                .FirstOrDefaultAsync();
            return result;
        }
        catch
        {
            return 0;
        }
    }
}
