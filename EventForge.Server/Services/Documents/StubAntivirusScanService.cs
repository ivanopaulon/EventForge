namespace EventForge.Server.Services.Documents;

/// <summary>
/// Stub implementation of antivirus scanning service for development/testing
/// </summary>
public class StubAntivirusScanService : IAntivirusScanService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StubAntivirusScanService> _logger;

    public StubAntivirusScanService(
        IConfiguration configuration,
        ILogger<StubAntivirusScanService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether antivirus scanning is enabled from configuration
    /// </summary>
    public bool IsEnabled => _configuration.GetValue<bool>("AntivirusScan:Enabled", false);

    /// <summary>
    /// Performs a mock antivirus scan for development purposes
    /// </summary>
    public async Task<AntivirusScanResult> ScanFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Antivirus scanning is disabled - skipping scan for file {FileName}", fileName);
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
        var scanDelayMs = _configuration.GetValue<int>("AntivirusScan:MockDelayMs", 100);
        if (scanDelayMs > 0)
        {
            await Task.Delay(scanDelayMs, cancellationToken);
        }

        // Mock threat detection based on filename patterns (for testing)
        var mockThreats = _configuration.GetSection("AntivirusScan:MockThreats").Get<string[]>() ?? Array.Empty<string>();
        var detectedThreats = new List<string>();
        var isClean = true;

        foreach (var threat in mockThreats)
        {
            if (fileName.Contains(threat, StringComparison.OrdinalIgnoreCase))
            {
                detectedThreats.Add($"Mock.{threat}.Detected");
                isClean = false;
                _logger.LogWarning("Mock threat detected in file {FileName}: {Threat}", fileName, threat);
            }
        }

        if (isClean)
        {
            _logger.LogDebug("Mock antivirus scan completed - file {FileName} is clean", fileName);
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
}