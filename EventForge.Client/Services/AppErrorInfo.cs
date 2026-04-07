namespace EventForge.Client.Services;

/// <summary>
/// Holds enriched information about an application error for display in the error detail dialog.
/// </summary>
public class AppErrorInfo
{
    /// <summary>Key used to store/retrieve ProblemDetailsDto in HttpRequestException.Data.</summary>
    public const string ProblemDetailsDataKey = "ProblemDetails";

    /// <summary>User-friendly error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Additional technical detail (e.g. ProblemDetails.Detail).</summary>
    public string? Details { get; set; }

    /// <summary>Exception message from the caught exception.</summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>Exception type name (e.g. "HttpRequestException").</summary>
    public string? ExceptionType { get; set; }

    /// <summary>Server-side correlation ID for log tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>When the error occurred.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
