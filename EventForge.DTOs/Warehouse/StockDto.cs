using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for stock information.
    /// </summary>
    public class StockDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        [Required]
        public Guid StorageLocationId { get; set; }
        public string? StorageLocationCode { get; set; }

        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReservedQuantity { get; set; }

        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        [Range(0, double.MaxValue)]
        public decimal? MinimumLevel { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaximumLevel { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReorderPoint { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReorderQuantity { get; set; }

        public DateTime? LastMovementDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        public decimal? TotalValue => Quantity * (UnitCost ?? 0);

        public DateTime? LastInventoryDate { get; set; }

        [StringLength(200)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}