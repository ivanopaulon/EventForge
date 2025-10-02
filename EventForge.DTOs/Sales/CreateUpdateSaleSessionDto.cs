using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{
    /// <summary>
    /// DTO for creating a new sale session.
    /// </summary>
    public class CreateSaleSessionDto
    {
        /// <summary>
        /// Operator (cashier) identifier.
        /// </summary>
        [Required(ErrorMessage = "Operator ID is required")]
        public Guid OperatorId { get; set; }

        /// <summary>
        /// POS terminal identifier.
        /// </summary>
        [Required(ErrorMessage = "POS ID is required")]
        public Guid PosId { get; set; }

        /// <summary>
        /// Customer identifier (optional for quick sales).
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Sale type (e.g., "RETAIL", "BAR", "RESTAURANT").
        /// </summary>
        [MaxLength(50)]
        public string? SaleType { get; set; }

        /// <summary>
        /// Table identifier (for bar/restaurant scenarios).
        /// </summary>
        public Guid? TableId { get; set; }

        /// <summary>
        /// Currency code (ISO 4217).
        /// </summary>
        [MaxLength(3)]
        public string Currency { get; set; } = "EUR";
    }

    /// <summary>
    /// DTO for updating a sale session.
    /// </summary>
    public class UpdateSaleSessionDto
    {
        /// <summary>
        /// Customer identifier.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Sale type.
        /// </summary>
        [MaxLength(50)]
        public string? SaleType { get; set; }

        /// <summary>
        /// Session status.
        /// </summary>
        public SaleSessionStatusDto? Status { get; set; }
    }

    /// <summary>
    /// DTO for sale session status.
    /// </summary>
    public enum SaleSessionStatusDto
    {
        Open = 0,
        Suspended = 1,
        Closed = 2,
        Cancelled = 3,
        Splitting = 4,
        Merging = 5
    }
}
