using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Service for sanitizing log entries to remove sensitive information
/// before exposing them to non-admin users.
/// </summary>
public interface ILogSanitizationService
{
    /// <summary>
    /// Sanitizes a collection of system logs by removing or masking sensitive information.
    /// </summary>
    /// <param name="logs">Collection of system logs to sanitize.</param>
    /// <returns>Collection of sanitized logs suitable for public viewing.</returns>
    IEnumerable<SanitizedSystemLogDto> SanitizeLogs(IEnumerable<SystemLogDto> logs);

    /// <summary>
    /// Sanitizes a single system log entry by removing or masking sensitive information.
    /// </summary>
    /// <param name="log">System log to sanitize.</param>
    /// <returns>Sanitized log suitable for public viewing.</returns>
    SanitizedSystemLogDto SanitizeLog(SystemLogDto log);
}
