using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for stock movement information.
    /// </summary>
    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public string MovementType { get; set; } = string.Empty;

        [Required]
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        public Guid? SerialId { get; set; }
        public string? SerialNumber { get; set; }

        public Guid? FromLocationId { get; set; }
        public string? FromLocationCode { get; set; }
        public string? FromWarehouseName { get; set; }

        public Guid? ToLocationId { get; set; }
        public string? ToLocationCode { get; set; }
        public string? ToWarehouseName { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        public decimal? TotalValue => Math.Abs(Quantity) * (UnitCost ?? 0);

        [Required]
        public DateTime MovementDate { get; set; }

        public Guid? DocumentHeaderId { get; set; }
        public string? DocumentReference { get; set; }

        public Guid? DocumentRowId { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? UserId { get; set; }

        [StringLength(50)]
        public string? Reference { get; set; }

        public Guid? MovementPlanId { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}