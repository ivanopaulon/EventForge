using System.Text.RegularExpressions;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Implementation of log sanitization service that removes or masks sensitive information
/// from log entries before exposing them to non-admin users.
/// </summary>
public class LogSanitizationService : ILogSanitizationService
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex IpAddressRegex = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex PathRegex = new(@"[A-Za-z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*", RegexOptions.Compiled);
    private static readonly Regex UnixPathRegex = new(@"/(?:[^/\0]+/)*[^/\0]*", RegexOptions.Compiled);

    // Sensitive property keys that should be excluded
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "token",
        "secret",
        "apikey",
        "connectionstring",
        "credential",
        "authorization",
        "sessionid",
        "traceid",
        "ipaddress",
        "useragent"
    };

    private readonly ILogger<LogSanitizationService> _logger;

    public LogSanitizationService(ILogger<LogSanitizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IEnumerable<SanitizedSystemLogDto> SanitizeLogs(IEnumerable<SystemLogDto> logs)
    {
        ArgumentNullException.ThrowIfNull(logs);

        return logs.Select(SanitizeLog);
    }

    /// <inheritdoc />
    public SanitizedSystemLogDto SanitizeLog(SystemLogDto log)
    {
        ArgumentNullException.ThrowIfNull(log);

        try
        {
            return new SanitizedSystemLogDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Level = log.Level ?? "Unknown",
                Message = SanitizeMessage(log.Message),
                Category = SanitizeCategory(log.Category),
                Source = SanitizeSource(log.Source),
                HasException = !string.IsNullOrEmpty(log.Exception),
                PublicProperties = FilterAndSanitizeProperties(log.Properties)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sanitizing log entry {LogId}, returning minimal sanitized log", log.Id);

            // Return a minimal sanitized log in case of errors
            return new SanitizedSystemLogDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Level = log.Level ?? "Unknown",
                Message = "Log entry could not be sanitized",
                HasException = !string.IsNullOrEmpty(log.Exception)
            };
        }
    }

    /// <summary>
    /// Sanitizes a log message by masking sensitive information.
    /// </summary>
    private string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        // Mask email addresses
        message = EmailRegex.Replace(message, "***@***.***");

        // Mask IP addresses
        message = IpAddressRegex.Replace(message, "***.***.***.***");

        // Mask Windows file paths
        message = PathRegex.Replace(message, "[PATH]");

        // Mask Unix file paths (be careful not to mask URLs)
        if (!message.Contains("http://") && !message.Contains("https://"))
        {
            message = UnixPathRegex.Replace(message, m =>
            {
                // Only replace if it looks like a file system path (contains multiple segments)
                return m.Value.Split('/').Length > 2 ? "[PATH]" : m.Value;
            });
        }

        return message;
    }

    /// <summary>
    /// Sanitizes the category by removing system-specific paths.
    /// </summary>
    private string? SanitizeCategory(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return null;
        }

        // Remove namespace paths, keep only the last component
        var parts = category.Split('.');
        return parts.Length > 0 ? parts[^1] : category;
    }

    /// <summary>
    /// Sanitizes the source by removing sensitive path information.
    /// </summary>
    private string? SanitizeSource(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        // Remove file paths and keep only the component name
        var parts = source.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : source;
    }

    /// <summary>
    /// Filters out sensitive properties and sanitizes the remaining ones.
    /// </summary>
    private Dictionary<string, string>? FilterAndSanitizeProperties(Dictionary<string, object>? properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return null;
        }

        var sanitized = new Dictionary<string, string>();

        foreach (var kvp in properties)
        {
            // Skip sensitive keys
            if (SensitiveKeys.Contains(kvp.Key))
            {
                continue;
            }

            // Convert value to string and sanitize
            var value = kvp.Value?.ToString() ?? string.Empty;
            sanitized[kvp.Key] = SanitizeMessage(value);
        }

        return sanitized.Count > 0 ? sanitized : null;
    }
}
