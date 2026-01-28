using System;

namespace EventForge.DTOs.Logging
{
    /// <summary>
    /// DTO for application log entries.
    /// </summary>
    public class ApplicationLogDto
    {
        /// <summary>
        /// Unique identifier for the log entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Timestamp when the log entry was created.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Log level (e.g., Information, Warning, Error).
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// Log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception details if applicable.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Machine name where the log was generated.
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// Username associated with the log entry.
        /// </summary>
        public string? UserName { get; set; }
    }
}
