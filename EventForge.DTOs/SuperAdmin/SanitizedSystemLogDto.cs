namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// Sanitized DTO for public system log entry viewing.
    /// Sensitive information is masked or removed for non-admin users.
    /// </summary>
    public class SanitizedSystemLogDto
    {
        /// <summary>
        /// Log entry identifier (preserved for reference).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Log level (Trace, Debug, Information, Warning, Error, Critical).
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// Sanitized log message (sensitive data masked).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Category/logger name (sanitized, system paths removed).
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Source component (sanitized).
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Indicates if an exception was logged (details not exposed).
        /// </summary>
        public bool HasException { get; set; }

        /// <summary>
        /// Public properties (filtered and sanitized).
        /// </summary>
        public Dictionary<string, string>? PublicProperties { get; set; }
    }
}
