using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.UpdateAgent.Pages;

/// <summary>
/// Log viewer page model for the Agent local web UI.
/// Reads the tail of the current Serilog rolling log file and parses
/// individual log entries, supporting level-based filtering.
/// </summary>
public class LogsModel(AgentOptions options, ILogger<LogsModel> logger) : PageModel
{
    public record LogEntry(string Timestamp, string Level, string Message, string? Exception);

    public string? LevelFilter { get; private set; }
    public string LogDirectory { get; private set; } = string.Empty;
    public string? LogFileName { get; private set; }
    public List<LogEntry> Lines { get; private set; } = [];
    public string InstallationId { get; private set; } = string.Empty;
    public string InstallationName { get; private set; } = string.Empty;

    private const int TailLines = 200;

    public void OnGet(string? level)
    {
        InstallationId   = options.InstallationId;
        InstallationName = options.InstallationName;
        LevelFilter = level ?? string.Empty;

        LogDirectory = !string.IsNullOrWhiteSpace(options.Logging.DirectoryPath)
            ? options.Logging.DirectoryPath
            : Path.Combine(AppContext.BaseDirectory, "logs");

        if (!Directory.Exists(LogDirectory))
        {
            logger.LogDebug("Log directory {Dir} not found", LogDirectory);
            return;
        }

        // Pick the most recently written log file
        var logFile = Directory.GetFiles(LogDirectory, "agent-*.log")
            .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (logFile is null) return;
        LogFileName = Path.GetFileName(logFile);

        // Read last TailLines lines safely (file may be locked by Serilog)
        string[] rawLines;
        try
        {
            using var fs = System.IO.File.Open(logFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            rawLines = sr.ReadToEnd()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(TailLines * 3) // read extra to account for multi-line entries
                .ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read log file {File}", logFile);
            return;
        }

        var parsed = ParseSerilogLines(rawLines);

        Lines = string.IsNullOrEmpty(LevelFilter)
            ? parsed.TakeLast(TailLines).ToList()
            : parsed.Where(e => e.Level.Equals(LevelFilter, StringComparison.OrdinalIgnoreCase))
                    .TakeLast(TailLines)
                    .ToList();
    }

    /// <summary>
    /// Parses Serilog compact text format: [HH:mm:ss LVL] Message [ExceptionLine...]
    /// Falls back to a best-effort parse for non-standard lines.
    /// </summary>
    private static List<LogEntry> ParseSerilogLines(string[] lines)
    {
        var result = new List<LogEntry>(lines.Length);
        string? pendingTs = null, pendingLevel = null, pendingMsg = null;
        var pendingEx = new System.Text.StringBuilder();

        foreach (var raw in lines)
        {
            // Serilog default format starts with [HH:mm:ss LVL]
            if (raw.Length > 14 && raw[0] == '[' && raw[9] == ' ')
            {
                // Flush previous
                if (pendingTs is not null)
                    result.Add(new LogEntry(pendingTs, pendingLevel!, pendingMsg!, pendingEx.Length > 0 ? pendingEx.ToString().Trim() : null));

                try
                {
                    var ts    = raw[1..9];
                    var lvlEnd = raw.IndexOf(']', 10);
                    var lvl   = lvlEnd > 0 ? raw[10..lvlEnd].Trim() : "INF";
                    var msg   = lvlEnd > 0 && lvlEnd + 2 < raw.Length ? raw[(lvlEnd + 2)..] : raw;

                    pendingTs    = ts;
                    pendingLevel = NormalizeLevel(lvl);
                    pendingMsg   = msg;
                    pendingEx.Clear();
                }
                catch
                {
                    pendingTs    = "?";
                    pendingLevel = "INF";
                    pendingMsg   = raw;
                    pendingEx.Clear();
                }
            }
            else if (pendingTs is not null)
            {
                // Continuation line (exception stack trace)
                pendingEx.AppendLine(raw);
            }
            else
            {
                result.Add(new LogEntry("", "INF", raw, null));
            }
        }

        if (pendingTs is not null)
            result.Add(new LogEntry(pendingTs, pendingLevel!, pendingMsg!, pendingEx.Length > 0 ? pendingEx.ToString().Trim() : null));

        return result;
    }

    private static string NormalizeLevel(string raw) => raw.ToUpperInvariant() switch
    {
        "VRB" or "VERBOSE"  => "VRB",
        "DBG" or "DEBUG"    => "DBG",
        "INF" or "INFORMATION" or "INFO" => "INF",
        "WRN" or "WARNING" or "WARN"     => "WRN",
        "ERR" or "ERROR"    => "ERR",
        "FTL" or "FATAL"    => "FTL",
        _                   => raw.Length > 3 ? raw[..3].ToUpperInvariant() : raw.ToUpperInvariant()
    };
}
