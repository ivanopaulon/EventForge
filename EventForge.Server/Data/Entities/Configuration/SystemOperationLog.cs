using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Audit log for system operations including configuration changes, migrations,
/// restarts, and other critical system events.
/// </summary>
public class SystemOperationLog
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of operation (e.g., ConfigChange, Migration, Restart, KeyRotation).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (e.g., configuration key name, migration name).
    /// </summary>
    [MaxLength(100)]
    public string? EntityType { get; set; }

    /// <summary>
    /// Identifier of the affected entity.
    /// </summary>
    [MaxLength(200)]
    public string? EntityId { get; set; }

    /// <summary>
    /// Action performed (e.g., Create, Update, Delete, Apply, Rollback).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the operation.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Previous value before the operation (for updates).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after the operation (for updates/creates).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Additional operation details in JSON format.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Indicates if the operation completed successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Date and time when the operation was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User or system that executed the operation.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ExecutedBy { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the user who initiated the operation.
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client that initiated the operation.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}
