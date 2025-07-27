using System;

namespace EventForge.DTOs.Audit
{
    /// <summary>
    /// DTO for entity change log entries.
    /// </summary>
    public class EntityChangeLogDto
    {
        /// <summary>
        /// Unique identifier for the log entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the entity class that was changed.
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name for the entity (for UI purposes).
        /// </summary>
        public string? EntityDisplayName { get; set; }

        /// <summary>
        /// Primary key of the entity that was changed.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Name of the property that was changed.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Type of operation performed (Insert, Update, Delete).
        /// </summary>
        public string OperationType { get; set; } = "Update";

        /// <summary>
        /// Previous value of the property.
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// New value of the property.
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// User who performed the change.
        /// </summary>
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date and time when the change was made (UTC).
        /// </summary>
        public DateTime ChangedAt { get; set; }
    }
}