namespace EventForge.Server.Services.Documents;

/// <summary>
/// Stub implementation of antivirus scanning service for development/testing
/// </summary>
public class StubAntivirusScanService(
    IConfiguration configuration,
    ILogger<StubAntivirusScanService> logger) : IAntivirusScanService
{

    /// <summary>
    /// Gets whether antivirus scanning is enabled from configuration
    /// </summary>
    public bool IsEnabled => configuration.GetValue<bool>("AntivirusScan:Enabled", false);

    /// <summary>
    /// Performs a mock antivirus scan for development purposes
    /// </summary>
    public async Task<AntivirusScanResult> ScanFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsEnabled)
            {
                logger.LogDebug("Antivirus scanning is disabled - skipping scan for file {FileName}", fileName);
                return new AntivirusScanResult
                {
                    IsClean = true,
                    ScanEngineInfo = "AntivirusScan:Disabled",
                    ScannedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ScannedBytes"] = fileStream.Length,
                        ["ScanMode"] = "Disabled"
                    }
                };
            }

            // Simulate scan delay
            var scanDelayMs = configuration.GetValue<int>("AntivirusScan:MockDelayMs", 100);
            if (scanDelayMs > 0)
            {
                await Task.Delay(scanDelayMs, cancellationToken);
            }

            // Mock threat detection based on filename patterns (for testing)
            var mockThreats = configuration.GetSection("AntivirusScan:MockThreats").Get<string[]>() ?? Array.Empty<string>();
            var detectedThreats = new List<string>();
            var isClean = true;

            foreach (var threat in mockThreats)
            {
                if (fileName.Contains(threat, StringComparison.OrdinalIgnoreCase))
                {
                    detectedThreats.Add($"Mock.{threat}.Detected");
                    isClean = false;
                    logger.LogWarning("Mock threat detected in file {FileName}: {Threat}", fileName, threat);
                }
            }

            if (isClean)
            {
                logger.LogDebug("Mock antivirus scan completed - file {FileName} is clean", fileName);
            }

            return new AntivirusScanResult
            {
                IsClean = isClean,
                DetectedThreats = detectedThreats,
                ScanEngineInfo = "Stub Antivirus Scanner v1.0",
                ScannedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["ScannedBytes"] = fileStream.Length,
                    ["ScanMode"] = "Mock",
                    ["ScanDurationMs"] = scanDelayMs
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ScanFileAsync for file {FileName}.", fileName);
            throw;
        }
    }

}
