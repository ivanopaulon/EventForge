using System.Text.Json;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Implementation of port configuration service.
/// </summary>
public class PortConfigurationService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<PortConfigurationService> logger) : IPortConfigurationService
{

    public string DetectEnvironment()
    {
        var serverType = configuration["ASPNETCORE_SERVER"] ?? string.Empty;

        if (serverType.Contains("IIS", StringComparison.OrdinalIgnoreCase) ||
            Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH") is not null)
        {
            logger.LogDebug("Detected IIS environment");
            return "IIS";
        }

        logger.LogDebug("Detected Kestrel environment");
        return "Kestrel";
    }

    public Dictionary<string, int?> ReadPortConfiguration()
    {
        var config = new Dictionary<string, int?>();

        var urls = configuration["Urls"];
        if (!string.IsNullOrEmpty(urls))
        {
            var urlList = urls.Split(';');
            foreach (var url in urlList)
            {
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        config["HTTP"] = uri.Port;
                    }
                }
                else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        config["HTTPS"] = uri.Port;
                    }
                }
            }
        }
        else
        {
            // Default Kestrel ports
            config["HTTP"] = 5000;
            config["HTTPS"] = 5001;
        }

        logger.LogDebug("Port configuration: {Config}", JsonSerializer.Serialize(config));
        return config;
    }

    public async Task WritePortConfigurationAsync(int? httpPort, int? httpsPort, CancellationToken cancellationToken = default)
    {
        try
        {
            var overridesPath = Path.Combine(environment.ContentRootPath, "appsettings.overrides.json");

            Dictionary<string, object>? existingConfig = null;
            if (File.Exists(overridesPath))
            {
                var existingJson = await File.ReadAllTextAsync(overridesPath, cancellationToken);
                existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
            }

            existingConfig ??= new Dictionary<string, object>();

            var urls = new List<string>();
            if (httpPort.HasValue)
            {
                urls.Add($"http://localhost:{httpPort.Value}");
            }
            if (httpsPort.HasValue)
            {
                urls.Add($"https://localhost:{httpsPort.Value}");
            }

            if (urls.Any())
            {
                existingConfig["Urls"] = string.Join(";", urls);
            }

            var json = JsonSerializer.Serialize(existingConfig, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(overridesPath, json, cancellationToken);

            logger.LogInformation("Port configuration saved: HTTP={HttpPort}, HTTPS={HttpsPort}", httpPort, httpsPort);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
