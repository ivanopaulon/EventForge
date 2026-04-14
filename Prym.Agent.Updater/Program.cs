// Prym.Agent.Updater
// ===================
// External updater process for silent, automatic Prym.Agent self-update.
//
// Best-practice pattern for Windows service self-update:
//   A running service cannot overwrite its own loaded binaries.
//   The service therefore launches this separate process BEFORE stopping itself.
//   This process waits for the service to fully stop, copies the new binaries into
//   the install directory (honouring the preserve list), then restarts the service.
//   On next startup the Agent reads the self-update marker file, reports the
//   result to the Hub via SignalR, and deletes the marker.
//
// Usage:
//   Prym.Agent.Updater.exe
//       --service  <Windows-service-name>   (e.g. "Prym Agent")
//       --source   <new-binaries-dir>       (extracted binaries/ from the update zip)
//       --target   <agent-install-dir>      (AppContext.BaseDirectory of the Agent)
//       [--preserve <pattern> ...]          (glob patterns — files that must NOT be overwritten)
//       [--cleanup  <temp-dir>]             (parent of --source; deleted after copy succeeds)
//
// Exit codes: 0 = success, 1 = argument error, 2 = service stop timeout, 3 = copy error, 4 = service start error.

using System.ServiceProcess;
using System.Text.RegularExpressions;

const int ExitOk             = 0;
const int ExitArgError       = 1;
const int ExitStopTimeout    = 2;
const int ExitCopyError      = 3;
const int ExitStartError     = 4;

// ── Parse args ───────────────────────────────────────────────────────────────
string? serviceName = null;
string? sourceDir   = null;
string? targetDir   = null;
string? cleanupDir  = null;
var preservePatterns = new List<string> { "appsettings.json", "appsettings.*.json", "pending.json" };

for (var i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--service":
            serviceName = args[++i]; break;
        case "--source":
            sourceDir = args[++i]; break;
        case "--target":
            targetDir = args[++i]; break;
        case "--cleanup":
            cleanupDir = args[++i]; break;
        case "--preserve":
            // Collect all following non-flag tokens as preserve patterns.
            // The while loop advances i to the last consumed pattern token;
            // the outer for-loop's i++ then steps past it to the next flag.
            while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                i++;
                preservePatterns.Add(args[i]);
            }
            break;
    }
}

if (string.IsNullOrWhiteSpace(serviceName) ||
    string.IsNullOrWhiteSpace(sourceDir)   ||
    string.IsNullOrWhiteSpace(targetDir))
{
    Console.Error.WriteLine("Usage: Prym.Agent.Updater --service <name> --source <dir> --target <dir> [--preserve <glob>...] [--cleanup <dir>]");
    return ExitArgError;
}

// ── Logging ───────────────────────────────────────────────────────────────────
// Write a small log file next to the target directory so problems can be diagnosed
// without needing the Agent's Serilog infrastructure.
var logPath = Path.Combine(targetDir, "self-update-log.txt");

void Log(string level, string message)
{
    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
    Console.WriteLine(line);
    try
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
    }
    catch (Exception logEx)
    {
        // Log file write failed — write to stderr so the operator can diagnose the problem.
        Console.Error.WriteLine($"[WARN] Could not write to log file '{logPath}': {logEx.Message}");
    }
}

Log("INFO", $"Prym.Agent.Updater started. Service='{serviceName}' Source='{sourceDir}' Target='{targetDir}'");

// ── Wait for the service to stop ──────────────────────────────────────────────
try
{
    using var sc = new ServiceController(serviceName);

    if (sc.Status != ServiceControllerStatus.Stopped &&
        sc.Status != ServiceControllerStatus.StopPending)
    {
        // Service might still be shutting down gracefully — just wait.
        Log("INFO", $"Service '{serviceName}' status: {sc.Status}. Waiting up to 60 s for it to stop…");
    }

    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
    Log("INFO", $"Service '{serviceName}' has stopped.");
}
catch (System.ComponentModel.Win32Exception ex)
{
    // Service not found or access denied.
    Log("WARN", $"Could not query service '{serviceName}': {ex.Message}. Proceeding with file copy anyway.");
}
catch (System.TimeoutException)
{
    Log("ERROR", $"Timeout waiting for service '{serviceName}' to stop. Aborting update.");
    return ExitStopTimeout;
}

// ── Copy binaries ─────────────────────────────────────────────────────────────
if (!Directory.Exists(sourceDir))
{
    Log("ERROR", $"Source directory not found: {sourceDir}");
    return ExitCopyError;
}

Directory.CreateDirectory(targetDir);

try
{
    // Build a set of file names (lower-cased) that must NOT be overwritten in the target.
    bool IsPreserved(string fileName)
    {
        foreach (var pattern in preservePatterns)
        {
            if (GlobMatches(pattern, Path.GetFileName(fileName)))
                return true;
        }
        return false;
    }

    var copied  = 0;
    var skipped = 0;

    foreach (var srcFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(sourceDir, srcFile);
        var dstFile  = Path.Combine(targetDir, relative);

        var dstDir = Path.GetDirectoryName(dstFile);
        if (dstDir is not null && !Directory.Exists(dstDir))
            Directory.CreateDirectory(dstDir);

        if (IsPreserved(relative) && File.Exists(dstFile))
        {
            Log("INFO", $"  PRESERVE: {relative}");
            skipped++;
            continue;
        }

        File.Copy(srcFile, dstFile, overwrite: true);
        Log("INFO", $"  COPY: {relative}");
        copied++;
    }

    Log("INFO", $"Copy completed. {copied} file(s) copied, {skipped} preserved.");
}
catch (Exception ex)
{
    Log("ERROR", $"File copy failed: {ex.Message}");
    return ExitCopyError;
}

// ── Clean up the temp source directory ───────────────────────────────────────
if (!string.IsNullOrWhiteSpace(cleanupDir) && Directory.Exists(cleanupDir))
{
    try
    {
        Directory.Delete(cleanupDir, recursive: true);
        Log("INFO", $"Temp directory deleted: {cleanupDir}");
    }
    catch (Exception ex)
    {
        Log("WARN", $"Could not delete temp directory '{cleanupDir}': {ex.Message} (non-fatal)");
    }
}

// ── Start the service ─────────────────────────────────────────────────────────
try
{
    using var sc = new ServiceController(serviceName);
    if (sc.Status == ServiceControllerStatus.Stopped)
    {
        Log("INFO", $"Starting service '{serviceName}'…");
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
        Log("INFO", $"Service '{serviceName}' started successfully.");
    }
    else
    {
        Log("WARN", $"Service '{serviceName}' is in state '{sc.Status}' — not starting from Updater.");
    }
}
catch (System.ComponentModel.Win32Exception ex)
{
    Log("ERROR", $"Could not start service '{serviceName}': {ex.Message}");
    return ExitStartError;
}
catch (System.TimeoutException)
{
    Log("ERROR", $"Timeout waiting for service '{serviceName}' to reach Running state.");
    return ExitStartError;
}

Log("INFO", "Prym.Agent.Updater finished successfully.");
return ExitOk;

// ── Helpers ───────────────────────────────────────────────────────────────────

/// <summary>
/// Matches a file name against a simple glob pattern (* matches any sequence of
/// characters that does not include a directory separator).
/// Examples: "appsettings.*.json", "pending.json"
/// </summary>
static bool GlobMatches(string pattern, string fileName)
{
    // Escape regex meta-chars except *, then replace * with .*
    var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
    return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
}
