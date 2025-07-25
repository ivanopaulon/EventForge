using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Health
{
    /// <summary>
    /// Basic health status information.
    /// </summary>
    public class HealthStatusDto
    {
        /// <summary>
        /// API health status.
        /// </summary>
        public string ApiStatus { get; set; } = string.Empty;

        /// <summary>
        /// Database health status.
        /// </summary>
        public string DatabaseStatus { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the health check (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// API version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Error message if any issues occurred.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Authentication system status.
        /// </summary>
        public string? AuthenticationStatus { get; set; }
    }

    /// <summary>
    /// Detailed health status information.
    /// </summary>
    public class DetailedHealthStatusDto : HealthStatusDto
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Machine name.
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// Process ID.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Working set memory usage.
        /// </summary>
        public long WorkingSet { get; set; }

        /// <summary>
        /// Application uptime.
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Database connection details.
        /// </summary>
        public Dictionary<string, object>? DatabaseDetails { get; set; }

        /// <summary>
        /// Status of dependencies.
        /// </summary>
        public Dictionary<string, string>? Dependencies { get; set; }

        /// <summary>
        /// Authentication configuration details.
        /// </summary>
        public Dictionary<string, object>? AuthenticationDetails { get; set; }

        /// <summary>
        /// List of all applied database migrations.
        /// </summary>
        public IEnumerable<string> AppliedMigrations { get; set; } = new List<string>();
    }
}