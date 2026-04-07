namespace Prym.Client.Services;

/// <summary>
/// Centralized service for displaying application notifications (snackbars) 
/// with enriched error details support.
/// </summary>
public interface IAppNotificationService
{
    /// <summary>Shows a success notification.</summary>
    void ShowSuccess(string message);

    /// <summary>Shows an informational notification.</summary>
    void ShowInfo(string message);

    /// <summary>Shows a warning notification.</summary>
    void ShowWarning(string message, string? details = null, string? correlationId = null);

    /// <summary>Shows an error notification. When details or exception are provided, adds a "Dettagli" action to open the error detail dialog.</summary>
    void ShowError(string message, string? details = null, Exception? exception = null, string? correlationId = null);

    /// <summary>Shows an error notification from an HttpRequestException, extracting ProblemDetails context when available.</summary>
    void ShowHttpError(string message, HttpRequestException? ex = null);
}
