using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventForge.UpdateHub.Configuration;

namespace EventForge.UpdateHub.Pages;

/// <summary>
/// Log viewer page model for the Hub web UI.
/// Reads the tail of the current Serilog log file and parses log level,
/// supporting level-based filtering (All / Error / Warning / Info / Debug).
/// </summary>
public class LogsModel(
    UpdateHubOptions hubOptions,
    IWebHostEnvironment env,
    ILogger<LogsModel> logger) : PageModel
{
    private const int MaxLines = 200;

    public IReadOnlyList<LogLine> Lines { get; private set; } = [];
    public string FilterLevel { get; private set; } = "All";

    public void OnGet(string? level = null)
    {
        FilterLevel = level ?? "All";
        Lines = ReadLines(FilterLevel);
    }

    // Called by auto-refresh fetch (/api/hub/log-lines?level=...)
    public IActionResult OnGetLines(string? level = null)
    {
        var lines = ReadLines(level ?? "All");
        return new JsonResult(lines);
    }

    private IReadOnlyList<LogLine> ReadLines(string levelFilter)
    {
        var logDir = string.IsNullOrWhiteSpace(hubOptions.Logging.DirectoryPath)
            ? Path.Combine(env.ContentRootPath, "logs")
            : hubOptions.Logging.DirectoryPath;

        if (!Directory.Exists(logDir))
            return [];

        var logFile = Directory.GetFiles(logDir, "*.log")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (logFile is null)
            return [];

        try
        {
            string[] allLines;
            using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
                allLines = sr.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var result = allLines
                .Reverse()
                .Take(MaxLines * 3)
                .Select(ParseLine)
                .Where(l => levelFilter == "All" || string.Equals(l.Level, levelFilter, StringComparison.OrdinalIgnoreCase))
                .Take(MaxLines)
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot read log file {File}", logFile);
            return [];
        }
    }

    private static LogLine ParseLine(string raw)
    {
        // Serilog default format: [timestamp] [LEVEL] message
        var level = "Information";
        if (raw.Contains("[ERR]") || raw.Contains("[Error]") || raw.Contains("\"Level\":\"Error\"")) level = "Error";
        else if (raw.Contains("[WRN]") || raw.Contains("[Warning]") || raw.Contains("\"Level\":\"Warning\"")) level = "Warning";
        else if (raw.Contains("[DBG]") || raw.Contains("[Debug]") || raw.Contains("\"Level\":\"Debug\"")) level = "Debug";
        else if (raw.Contains("[VRB]") || raw.Contains("[Verbose]")) level = "Verbose";
        else if (raw.Contains("[FTL]") || raw.Contains("[Fatal]")) level = "Fatal";

        return new LogLine(raw.Trim(), level);
    }

    public record LogLine(string Text, string Level);
}
