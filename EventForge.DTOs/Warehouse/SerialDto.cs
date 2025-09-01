using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for serial number information.
    /// </summary>
    public class SerialDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        public Guid? CurrentLocationId { get; set; }
        public string? CurrentLocationCode { get; set; }
        public string? WarehouseName { get; set; }

        public string Status { get; set; } = "Available";

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }

        public Guid? OwnerId { get; set; }
        public string? OwnerName { get; set; }

        public DateTime? SaleDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        [StringLength(50)]
        public string? RfidTag { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}