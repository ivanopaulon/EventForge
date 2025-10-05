using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a table in a bar/restaurant scenario.
/// Supports reservations, status tracking, and historical data.
/// </summary>
public class TableSession : AuditableEntity
{
    /// <summary>
    /// Table unique identifier.
    /// </summary>
    public new Guid Id { get; set; }

    /// <summary>
    /// Table number or identifier.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = string.Empty;

    /// <summary>
    /// Table name/description.
    /// </summary>
    [MaxLength(100)]
    public string? TableName { get; set; }

    /// <summary>
    /// Table capacity (number of seats).
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Current table status.
    /// </summary>
    [Required]
    public TableStatus Status { get; set; } = TableStatus.Available;

    /// <summary>
    /// Current active sale session (if occupied).
    /// </summary>
    public Guid? CurrentSaleSessionId { get; set; }

    /// <summary>
    /// Current sale session navigation property.
    /// </summary>
    public SaleSession? CurrentSaleSession { get; set; }

    /// <summary>
    /// Area or zone where the table is located.
    /// </summary>
    [MaxLength(100)]
    public string? Area { get; set; }

    /// <summary>
    /// Position X coordinate for visual layout.
    /// </summary>
    public int? PositionX { get; set; }

    /// <summary>
    /// Position Y coordinate for visual layout.
    /// </summary>
    public int? PositionY { get; set; }

    /// <summary>
    /// Indicates if this table is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Reservations for this table.
    /// </summary>
    public ICollection<TableReservation> Reservations { get; set; } = new List<TableReservation>();
}

/// <summary>
/// Table status enumeration.
/// </summary>
public enum TableStatus
{
    /// <summary>
    /// Table is available for new customers.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Table is occupied.
    /// </summary>
    Occupied = 1,

    /// <summary>
    /// Table is reserved.
    /// </summary>
    Reserved = 2,

    /// <summary>
    /// Table is being cleaned.
    /// </summary>
    Cleaning = 3,

    /// <summary>
    /// Table is out of service.
    /// </summary>
    OutOfService = 4
}

/// <summary>
/// Represents a table reservation.
/// </summary>
public class TableReservation : AuditableEntity
{
    /// <summary>
    /// Reservation unique identifier.
    /// </summary>
    public new Guid Id { get; set; }

    /// <summary>
    /// Reference to the table.
    /// </summary>
    [Required]
    public Guid TableId { get; set; }

    /// <summary>
    /// Table navigation property.
    /// </summary>
    public TableSession? Table { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer phone number.
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Number of guests.
    /// </summary>
    public int NumberOfGuests { get; set; }

    /// <summary>
    /// Reservation date and time.
    /// </summary>
    [Required]
    public DateTime ReservationDateTime { get; set; }

    /// <summary>
    /// Reservation duration in minutes.
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Reservation status.
    /// </summary>
    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    /// <summary>
    /// Special requests or notes.
    /// </summary>
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }

    /// <summary>
    /// Confirmation timestamp.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Arrival timestamp.
    /// </summary>
    public DateTime? ArrivedAt { get; set; }
}

/// <summary>
/// Reservation status enumeration.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Reservation is pending confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reservation confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Customer has arrived.
    /// </summary>
    Arrived = 2,

    /// <summary>
    /// Reservation completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Reservation cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Customer did not show up (no-show).
    /// </summary>
    NoShow = 5
}
