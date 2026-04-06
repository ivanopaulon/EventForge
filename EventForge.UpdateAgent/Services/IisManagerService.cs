using System.Diagnostics;

namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Controls IIS application pools and sites via appcmd.exe.
/// Only used when Server component is enabled.
/// </summary>
public class IisManagerService(AgentOptions options, ILogger<IisManagerService> logger)
{
    private static readonly string AppCmdPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        @"inetsrv\appcmd.exe");

    public async Task StopSiteAsync(CancellationToken ct)
    {
        var siteName = options.Components.Server.IISSiteName;
        logger.LogInformation("Stopping IIS site: {SiteName}", siteName);
        await RunAppCmdAsync($"stop site \"{siteName}\"", ct);
        await RunAppCmdAsync($"stop apppool \"{options.Components.Server.AppPoolName}\"", ct);
    }

    public async Task StartSiteAsync(CancellationToken ct)
    {
        var siteName = options.Components.Server.IISSiteName;
        logger.LogInformation("Starting IIS site: {SiteName}", siteName);
        await RunAppCmdAsync($"start apppool \"{options.Components.Server.AppPoolName}\"", ct);
        await RunAppCmdAsync($"start site \"{siteName}\"", ct);
        // Give IIS time to warm up
        await Task.Delay(5000, ct);
    }

    private async Task RunAppCmdAsync(string arguments, CancellationToken ct)
    {
        if (!File.Exists(AppCmdPath))
        {
            logger.LogWarning("appcmd.exe not found at {Path}. IIS management unavailable.", AppCmdPath);
            return;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = AppCmdPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            logger.LogWarning("appcmd {Args} exited {Code}: {Error}", arguments, process.ExitCode, error);
        else
            logger.LogDebug("appcmd {Args}: {Output}", arguments, output.Trim());
    }
}
