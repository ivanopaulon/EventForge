using EventForge.Server.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Server page that lists all log backup directories created by <see cref="HostedServices.LogCleanupService"/>.
/// Allows the SuperAdmin to browse backup files, view their JSON contents and delete old backups.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class LogBackupsModel : PageModel
{
    private readonly IConfigurationService _configService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LogBackupsModel> _logger;

    // Maximum number of bytes loaded into the in-page viewer (1 MiB).
    private const long ViewLimitBytes = 1 * 1024 * 1024;

    public LogBackupsModel(
        IConfigurationService configService,
        IWebHostEnvironment env,
        ILogger<LogBackupsModel> logger)
    {
        _configService = configService;
        _env           = env;
        _logger        = logger;
    }

    // ── Page data ─────────────────────────────────────────────────────────────
    public List<BackupInfo> Backups { get; set; } = [];
    public string BackupDirectoryPath { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // ── File viewer state (populated by OnGetViewFileAsync) ───────────────────
    [BindProperty(SupportsGet = true)] public string? ViewBackupName  { get; set; }
    [BindProperty(SupportsGet = true)] public string? ViewFileName    { get; set; }
    public string? ViewFileContent   { get; set; }
    public bool    ViewFileTruncated { get; set; }

    // ── Inner models ──────────────────────────────────────────────────────────

    public sealed class BackupInfo
    {
        public string   DirectoryName   { get; init; } = string.Empty;
        public DateTime Timestamp       { get; init; }
        public long     TotalSizeBytes  { get; init; }
        public List<FileEntry> Files    { get; init; } = [];
    }

    public sealed class FileEntry
    {
        public string FileName   { get; init; } = string.Empty;
        public long   SizeBytes  { get; init; }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    public async Task OnGetAsync()
    {
        if (TempData["SuccessMessage"] is string ok)  SuccessMessage = ok;
        if (TempData["ErrorMessage"]   is string err) ErrorMessage   = err;

        BackupDirectoryPath = await ResolveBackupDirectoryAsync(HttpContext.RequestAborted);
        Backups = LoadBackups(BackupDirectoryPath);

        // If a file view was requested, load the content.
        if (!string.IsNullOrWhiteSpace(ViewBackupName) && !string.IsNullOrWhiteSpace(ViewFileName))
        {
            if (!TryResolveFilePath(BackupDirectoryPath, ViewBackupName, ViewFileName, out var filePath))
            {
                ErrorMessage = "File non trovato o percorso non valido.";
            }
            else
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    if (fi.Exists)
                    {
                        if (fi.Length > ViewLimitBytes)
                        {
                            // Read only the first ViewLimitBytes for display
                            var buffer = new byte[ViewLimitBytes];
                            await using var fs = System.IO.File.OpenRead(filePath);
                            var read = await fs.ReadAsync(buffer, HttpContext.RequestAborted);
                            ViewFileContent   = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                            ViewFileTruncated = true;
                        }
                        else
                        {
                            ViewFileContent   = await System.IO.File.ReadAllTextAsync(filePath, HttpContext.RequestAborted);
                            ViewFileTruncated = false;
                        }
                    }
                    else
                    {
                        ErrorMessage = "File non trovato.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read backup file {File}", filePath);
                    ErrorMessage = "Impossibile leggere il file.";
                }
            }
        }
    }

    /// <summary>Downloads an individual backup file as application/octet-stream.</summary>
    public async Task<IActionResult> OnGetDownloadFileAsync(string backupName, string fileName)
    {
        var baseDir = await ResolveBackupDirectoryAsync(HttpContext.RequestAborted);
        if (!TryResolveFilePath(baseDir, backupName, fileName, out var filePath) || !System.IO.File.Exists(filePath))
            return NotFound();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, HttpContext.RequestAborted);
        return File(fileBytes, "application/json", fileName);
    }

    /// <summary>Deletes an entire backup directory (all its files).</summary>
    public async Task<IActionResult> OnPostDeleteBackupAsync(string backupName)
    {
        var baseDir = await ResolveBackupDirectoryAsync(HttpContext.RequestAborted);

        // Validate the name: no path separators, no ".."
        if (!IsValidName(backupName) || !backupName.StartsWith("LogBackup_", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Nome backup non valido.";
            return RedirectToPage();
        }

        var dirPath = Path.Combine(baseDir, backupName);
        if (!Directory.Exists(dirPath))
        {
            TempData["ErrorMessage"] = "Backup non trovato.";
            return RedirectToPage();
        }

        try
        {
            Directory.Delete(dirPath, recursive: true);
            _logger.LogInformation("LogBackupsPage: backup {Backup} deleted by {User}.", backupName, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Backup '{backupName}' eliminato.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup directory {Dir}", dirPath);
            TempData["ErrorMessage"] = "Impossibile eliminare il backup.";
        }

        return RedirectToPage();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> ResolveBackupDirectoryAsync(CancellationToken ct)
    {
        var configured = await _configService.GetValueAsync("Logging.BackupDirectory", string.Empty, ct);
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(_env.ContentRootPath, "Backups")
            : configured;
    }

    private static List<BackupInfo> LoadBackups(string baseDir)
    {
        if (!Directory.Exists(baseDir))
            return [];

        var result = new List<BackupInfo>();

        foreach (var dir in Directory.EnumerateDirectories(baseDir, "LogBackup_*")
                                     .OrderByDescending(d => d))
        {
            var dirName  = Path.GetFileName(dir);
            var ts       = ParseTimestamp(dirName);
            var files    = new List<FileEntry>();

            foreach (var f in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
            {
                var fi = new FileInfo(f);
                files.Add(new FileEntry { FileName = fi.Name, SizeBytes = fi.Length });
            }

            result.Add(new BackupInfo
            {
                DirectoryName  = dirName,
                Timestamp      = ts,
                TotalSizeBytes = files.Sum(f => f.SizeBytes),
                Files          = files
            });
        }

        return result;
    }

    private static DateTime ParseTimestamp(string dirName)
    {
        // Expected format: LogBackup_yyyyMMdd_HHmmss
        const string prefix = "LogBackup_";
        if (dirName.Length > prefix.Length
            && DateTime.TryParseExact(
                dirName[prefix.Length..],
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt))
        {
            return dt;
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// Resolves and validates an absolute file path for a backup file.
    /// Returns false when the resulting path would escape the backup directory.
    /// </summary>
    private static bool TryResolveFilePath(string baseDir, string backupName, string fileName,
        out string filePath)
    {
        filePath = string.Empty;

        if (!IsValidName(backupName) || !IsValidName(fileName))
            return false;
        if (!backupName.StartsWith("LogBackup_", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(baseDir, backupName, fileName));
        var safeBase  = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(safeBase, StringComparison.OrdinalIgnoreCase))
            return false;

        filePath = candidate;
        return true;
    }

    private static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        // Disallow path separators and dot-dot traversal
        return !name.Contains('/') && !name.Contains('\\') && !name.Contains("..");
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024):F1} MB";
    }
}
