using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Sales
{
    /// <summary>
    /// Compact live status for a table in the daily POS flow.
    /// </summary>
    public class TableDailyStatusDto
    {
        public Guid TableId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string Status { get; set; } = "Available";
        public bool HasOpenBill { get; set; }
        public Guid? OpenSaleSessionId { get; set; }
        public decimal? CurrentPartialAmount { get; set; }
        public Guid? NextReservationId { get; set; }
        public DateTime? NextReservationTime { get; set; }
        public string? NextReservationCustomerName { get; set; }
        public int? MinutesUntilNextReservation { get; set; }
    }

    /// <summary>
    /// Aggregated daily flow payload for reservations and tables.
    /// </summary>
    public class DailyFlowDto
    {
        public List<TableReservationDto> TodayReservations { get; set; } = new();
        public List<TableDailyStatusDto> Tables { get; set; } = new();
    }

    /// <summary>
    /// Request payload for reservation check-in and table assignment/session opening.
    /// </summary>
    public class ReservationCheckInRequestDto
    {
        public Guid? SaleSessionId { get; set; }
        public Guid? OperatorId { get; set; }
        public Guid? PosId { get; set; }
        public Guid? CustomerId { get; set; }

        [MaxLength(50)]
        public string? SaleType { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "EUR";
    }

    /// <summary>
    /// Result payload returned after reservation check-in.
    /// </summary>
    public class ReservationCheckInResultDto
    {
        public Guid ReservationId { get; set; }
        public Guid TableId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public Guid SaleSessionId { get; set; }
        public TableReservationDto? Reservation { get; set; }
    }
}
