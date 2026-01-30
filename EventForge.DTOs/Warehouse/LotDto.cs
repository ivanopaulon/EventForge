using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for lot information.
    /// </summary>
    public class LotDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public DateTime? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OriginalQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AvailableQuantity { get; set; }

        public string Status { get; set; } = string.Empty;
        public string QualityStatus { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        [StringLength(50)]
        public string? CountryOfOrigin { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}