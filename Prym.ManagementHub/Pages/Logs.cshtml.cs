using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.ManagementHub.Pages;

/// <summary>
/// Log viewer page model for the Hub web UI.
/// Reads the tail of the selected Serilog daily log file and parses individual log entries
/// (timestamp / level / message / optional exception), supporting level-based filtering.
/// The parser is aligned with the Agent's structured parser for consistent UX.
/// </summary>
public class LogsModel(
    ManagementHubOptions hubOptions,
    IWebHostEnvironment env,
    ILogger<LogsModel> logger) : PageModel
{
    private const int TailEntries = 200;

    public IReadOnlyList<LogEntry> Lines { get; private set; } = [];
    public string FilterLevel { get; private set; } = "All";
    public string? LogFile { get; private set; }
    public IReadOnlyList<string> AvailableFiles { get; private set; } = [];
    public string LogDirectory { get; private set; } = string.Empty;

    public record LogEntry(string Timestamp, string Level, string Message, string? Exception);

    public void OnGet(string? level = null, string? file = null)
    {
        FilterLevel = level ?? "All";
        LogDirectory = ResolveLogDir();
        AvailableFiles = ListLogFiles();

        // Honour requested file, otherwise use the most-recently-written one
        if (!string.IsNullOrWhiteSpace(file) && AvailableFiles.Contains(file))
            LogFile = file;
        else
            LogFile = AvailableFiles.FirstOrDefault();

        Lines = LogFile is null ? [] : ReadEntries(LogFile, FilterLevel);
    }

    // Called by auto-refresh fetch: GET /logs?handler=Lines&level=...&file=...
    public IActionResult OnGetLines(string? level = null, string? file = null)
    {
        LogDirectory = ResolveLogDir();
        var availFiles = ListLogFiles();
        var target = (!string.IsNullOrWhiteSpace(file) && availFiles.Contains(file))
            ? file
            : availFiles.FirstOrDefault();

        var entries = target is null ? [] : ReadEntries(target, level ?? "All");

        // Return minimal JSON compatible with the client-side refresh script
        return new JsonResult(entries.Select(e => new
        {
            timestamp = e.Timestamp,
            level     = e.Level,
            message   = e.Message,
            exception = e.Exception
        }));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string ResolveLogDir() =>
        string.IsNullOrWhiteSpace(hubOptions.Logging.DirectoryPath)
            ? Path.Combine(env.ContentRootPath, "logs")
            : hubOptions.Logging.DirectoryPath;

    private IReadOnlyList<string> ListLogFiles()
    {
        if (!Directory.Exists(LogDirectory)) return [];
        return Directory.GetFiles(LogDirectory, "hub-*.log")
            .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToList();
    }

    private IReadOnlyList<LogEntry> ReadEntries(string fileName, string levelFilter)
    {
        var fullPath = Path.Combine(LogDirectory, fileName);
        string[] rawLines;
        try
        {
            using var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            rawLines = sr.ReadToEnd()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(TailEntries * 3)
                .ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot read Hub log file {File}", fullPath);
            return [];
        }

        var parsed = ParseEntries(rawLines);

        return levelFilter == "All"
            ? parsed.TakeLast(TailEntries).ToList()
            : parsed.Where(e => string.Equals(e.Level, levelFilter, StringComparison.OrdinalIgnoreCase))
                    .TakeLast(TailEntries)
                    .ToList();
    }

    /// <summary>
    /// Parses Serilog compact text format: <c>[HH:mm:ss LVL] Message</c>.
    /// Continuation lines (exception stack traces) are collected into the
    /// preceding entry's Exception field.
    /// </summary>
    private static List<LogEntry> ParseEntries(string[] lines)
    {
        var result = new List<LogEntry>(lines.Length);
        string? pendingTs = null, pendingLevel = null, pendingMsg = null;
        var pendingEx = new System.Text.StringBuilder();

        foreach (var raw in lines)
        {
            if (raw.Length > 14 && raw[0] == '[' && raw[9] == ' ')
            {
                // Flush previous entry
                if (pendingTs is not null)
                    result.Add(new LogEntry(pendingTs, pendingLevel!, pendingMsg!,
                        pendingEx.Length > 0 ? pendingEx.ToString().Trim() : null));

                try
                {
                    var ts     = raw[1..9];
                    var lvlEnd = raw.IndexOf(']', 10);
                    var lvl    = lvlEnd > 0 ? raw[10..lvlEnd].Trim() : "INF";
                    var msg    = lvlEnd > 0 && lvlEnd + 2 < raw.Length ? raw[(lvlEnd + 2)..] : raw;

                    pendingTs    = ts;
                    pendingLevel = NormalizeLevel(lvl);
                    pendingMsg   = msg;
                    pendingEx.Clear();
                }
                catch
                {
                    pendingTs    = "?";
                    pendingLevel = "Information";
                    pendingMsg   = raw;
                    pendingEx.Clear();
                }
            }
            else if (pendingTs is not null)
            {
                pendingEx.AppendLine(raw);
            }
            else
            {
                result.Add(new LogEntry(string.Empty, "Information", raw.Trim(), null));
            }
        }

        if (pendingTs is not null)
            result.Add(new LogEntry(pendingTs, pendingLevel!, pendingMsg!,
                pendingEx.Length > 0 ? pendingEx.ToString().Trim() : null));

        return result;
    }

    private static string NormalizeLevel(string raw) => raw.ToUpperInvariant() switch
    {
        "VRB" or "VERBOSE"                    => "Verbose",
        "DBG" or "DEBUG"                      => "Debug",
        "INF" or "INFORMATION" or "INFO"      => "Information",
        "WRN" or "WARNING"     or "WARN"      => "Warning",
        "ERR" or "ERROR"                      => "Error",
        "FTL" or "FATAL"                      => "Fatal",
        _                                     => raw.Length > 3 ? raw[..3] : raw
    };
}

