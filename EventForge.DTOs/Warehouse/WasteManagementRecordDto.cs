using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for waste management record information.
    /// </summary>
    public class WasteManagementRecordDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        public Guid? SerialId { get; set; }
        public string? SerialNumber { get; set; }

        public Guid? StorageLocationId { get; set; }
        public string? StorageLocationName { get; set; }

        [Required]
        [StringLength(50)]
        public string RecordNumber { get; set; } = string.Empty;

        [Required]
        public string WasteType { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WeightKg { get; set; }

        [Required]
        public DateTime WasteDate { get; set; }

        [Required]
        public string DisposalMethod { get; set; } = string.Empty;

        public DateTime? DisposalDate { get; set; }

        [StringLength(200)]
        public string? DisposalCompany { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DisposalCost { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsHazardous { get; set; }

        [StringLength(50)]
        public string? HazardCode { get; set; }

        [StringLength(1000)]
        public string? EnvironmentalImpact { get; set; }

        [Range(0, 100)]
        public decimal? RecyclingRatePercentage { get; set; }

        public decimal? RecoveryValue { get; set; }

        [StringLength(100)]
        public string? ResponsiblePerson { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public Guid? DocumentId { get; set; }

        [StringLength(100)]
        public string? CertificateNumber { get; set; }

        public bool IsCompliant { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for creating a waste management record.
    /// </summary>
    public class CreateWasteManagementRecordDto
    {
        public Guid? ProductId { get; set; }
        public Guid? LotId { get; set; }
        public Guid? SerialId { get; set; }
        public Guid? StorageLocationId { get; set; }

        [Required]
        [StringLength(50)]
        public string RecordNumber { get; set; } = string.Empty;

        [Required]
        public string WasteType { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WeightKg { get; set; }

        [Required]
        public DateTime WasteDate { get; set; }

        [Required]
        public string DisposalMethod { get; set; } = string.Empty;

        public DateTime? DisposalDate { get; set; }

        [StringLength(200)]
        public string? DisposalCompany { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DisposalCost { get; set; }

        public bool IsHazardous { get; set; }

        [StringLength(50)]
        public string? HazardCode { get; set; }

        [StringLength(1000)]
        public string? EnvironmentalImpact { get; set; }

        [Range(0, 100)]
        public decimal? RecyclingRatePercentage { get; set; }

        public decimal? RecoveryValue { get; set; }

        [StringLength(100)]
        public string? ResponsiblePerson { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public Guid? DocumentId { get; set; }

        [StringLength(100)]
        public string? CertificateNumber { get; set; }
    }

    /// <summary>
    /// DTO for updating a waste management record.
    /// </summary>
    public class UpdateWasteManagementRecordDto
    {
        public string? Status { get; set; }

        public DateTime? DisposalDate { get; set; }

        [StringLength(200)]
        public string? DisposalCompany { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DisposalCost { get; set; }

        [StringLength(1000)]
        public string? EnvironmentalImpact { get; set; }

        [Range(0, 100)]
        public decimal? RecyclingRatePercentage { get; set; }

        public decimal? RecoveryValue { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? CertificateNumber { get; set; }

        public bool? IsCompliant { get; set; }
    }
}
