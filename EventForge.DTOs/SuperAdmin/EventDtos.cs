using System;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// DTO for event management in SuperAdmin context.
    /// </summary>
    public class EventManagementDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Capacity { get; set; }
        public EventStatus Status { get; set; }
        public int TeamCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public Guid? TenantId { get; set; }
        public string? TenantName { get; set; }
    }

    /// <summary>
    /// DTO for creating events.
    /// </summary>
    public class CreateEventManagementDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ShortDescription { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string LongDescription { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; } = 1;

        [Required]
        public EventStatus Status { get; set; } = EventStatus.Planned;

        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// DTO for updating events.
    /// </summary>
    public class UpdateEventManagementDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ShortDescription { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string LongDescription { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Required]
        public EventStatus Status { get; set; }
    }

    /// <summary>
    /// DTO for event statistics.
    /// </summary>
    public class EventStatisticsDto
    {
        public int TotalEvents { get; set; }
        public int PlannedEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int CancelledEvents { get; set; }
        public int EventsThisMonth { get; set; }
        public int TotalCapacity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for event type management.
    /// </summary>
    public class EventTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int EventCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating event types.
    /// </summary>
    public class CreateEventTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#1976d2";

        [MaxLength(50)]
        public string Icon { get; set; } = "event";

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating event types.
    /// </summary>
    public class UpdateEventTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for event category management.
    /// </summary>
    public class EventCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int EventCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating event categories.
    /// </summary>
    public class CreateEventCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#1976d2";

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating event categories.
    /// </summary>
    public class UpdateEventCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}