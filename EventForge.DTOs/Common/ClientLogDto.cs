using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Represents a log entry sent from the client to the server.
    /// Maps to existing Serilog infrastructure without requiring new tables.
    /// </summary>
    public class ClientLogDto
    {
        /// <summary>
        /// Log level (Information, Warning, Error, Debug, Critical)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Level { get; set; } = "Information";

        /// <summary>
        /// Log message
        /// </summary>
        [Required]
        [StringLength(5000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Current page/route where the log occurred
        /// </summary>
        [StringLength(500)]
        public string? Page { get; set; }

        /// <summary>
        /// User ID (if authenticated)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Exception details (if applicable)
        /// </summary>
        [StringLength(10000)]
        public string? Exception { get; set; }

        /// <summary>
        /// Browser/client information
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Additional properties as JSON
        /// </summary>
        [StringLength(5000)]
        public string? Properties { get; set; }

        /// <summary>
        /// Client timestamp when the log was generated
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation ID for tracing across client and server
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Category/logger name
        /// </summary>
        [StringLength(200)]
        public string? Category { get; set; }
    }

    /// <summary>
    /// Batch request for sending multiple client logs
    /// </summary>
    public class ClientLogBatchDto
    {
        /// <summary>
        /// Collection of client log entries
        /// </summary>
        [Required]
        public List<ClientLogDto> Logs { get; set; } = new List<ClientLogDto>();

        /// <summary>
        /// Maximum batch size allowed
        /// </summary>
        public const int MaxBatchSize = 100;
    }
}