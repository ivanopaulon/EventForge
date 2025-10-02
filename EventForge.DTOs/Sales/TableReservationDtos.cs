using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{

    /// <summary>
    /// DTO for table reservation information.
    /// </summary>
    public class TableReservationDto
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int NumberOfGuests { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string Status { get; set; } = "Pending";
        public string? SpecialRequests { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new reservation.
    /// </summary>
    public class CreateTableReservationDto
    {
        [Required]
        public Guid TableId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [Range(1, 50)]
        public int NumberOfGuests { get; set; }

        [Required]
        public DateTime ReservationDateTime { get; set; }

        [Range(15, 480)]
        public int? DurationMinutes { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }
    }

    /// <summary>
    /// DTO for updating a reservation.
    /// </summary>
    public class UpdateTableReservationDto
    {
        [MaxLength(200)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Range(1, 50)]
        public int? NumberOfGuests { get; set; }

        public DateTime? ReservationDateTime { get; set; }

        [Range(15, 480)]
        public int? DurationMinutes { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }
    }
}
