using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for sustainability certificate information.
    /// </summary>
    public class SustainabilityCertificateDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        [Required]
        public string CertificateType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CertificateNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string IssuingAuthority { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string Status { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CountryOfOrigin { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CarbonFootprintKg { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WaterUsageLiters { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EnergyConsumptionKwh { get; set; }

        [Range(0, 100)]
        public decimal? RecycledContentPercentage { get; set; }

        public bool IsRecyclable { get; set; }
        public bool IsBiodegradable { get; set; }
        public bool IsOrganic { get; set; }
        public bool IsFairTrade { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public Guid? DocumentId { get; set; }

        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string? VerifiedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for creating a sustainability certificate.
    /// </summary>
    public class CreateSustainabilityCertificateDto
    {
        public Guid? ProductId { get; set; }
        public Guid? LotId { get; set; }

        [Required]
        public string CertificateType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CertificateNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string IssuingAuthority { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? Status { get; set; }

        [StringLength(100)]
        public string? CountryOfOrigin { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CarbonFootprintKg { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WaterUsageLiters { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EnergyConsumptionKwh { get; set; }

        [Range(0, 100)]
        public decimal? RecycledContentPercentage { get; set; }

        public bool IsRecyclable { get; set; }
        public bool IsBiodegradable { get; set; }
        public bool IsOrganic { get; set; }
        public bool IsFairTrade { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public Guid? DocumentId { get; set; }
    }

    /// <summary>
    /// DTO for updating a sustainability certificate.
    /// </summary>
    public class UpdateSustainabilityCertificateDto
    {
        [StringLength(100)]
        public string? CertificateNumber { get; set; }

        public string? Status { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CarbonFootprintKg { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WaterUsageLiters { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EnergyConsumptionKwh { get; set; }

        [Range(0, 100)]
        public decimal? RecycledContentPercentage { get; set; }

        public bool? IsRecyclable { get; set; }
        public bool? IsBiodegradable { get; set; }
        public bool? IsOrganic { get; set; }
        public bool? IsFairTrade { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool? IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string? VerifiedBy { get; set; }
    }
}
