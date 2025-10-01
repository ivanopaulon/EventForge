using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents sustainability and environmental certifications for products and lots.
/// </summary>
public class SustainabilityCertificate : AuditableEntity
{
    /// <summary>
    /// Product this certificate is for (optional - can be at lot level).
    /// </summary>
    [Display(Name = "Product", Description = "Product this certificate is for.")]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot this certificate is for (optional - can be at product level).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot this certificate is for.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Type of sustainability certification.
    /// </summary>
    [Required(ErrorMessage = "Certificate type is required.")]
    [Display(Name = "Certificate Type", Description = "Type of sustainability certification.")]
    public SustainabilityCertificateType CertificateType { get; set; }

    /// <summary>
    /// Certificate number or identifier.
    /// </summary>
    [Required(ErrorMessage = "Certificate number is required.")]
    [StringLength(100, ErrorMessage = "Certificate number cannot exceed 100 characters.")]
    [Display(Name = "Certificate Number", Description = "Certificate number or identifier.")]
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Issuing authority or organization.
    /// </summary>
    [Required(ErrorMessage = "Issuing authority is required.")]
    [StringLength(200, ErrorMessage = "Issuing authority cannot exceed 200 characters.")]
    [Display(Name = "Issuing Authority", Description = "Issuing authority or organization.")]
    public string IssuingAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Date when the certificate was issued.
    /// </summary>
    [Required(ErrorMessage = "Issue date is required.")]
    [Display(Name = "Issue Date", Description = "Date when the certificate was issued.")]
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Date when the certificate expires.
    /// </summary>
    [Display(Name = "Expiry Date", Description = "Date when the certificate expires.")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Status of the certificate.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the certificate.")]
    public CertificateStatus Status { get; set; } = CertificateStatus.Valid;

    /// <summary>
    /// Country of origin for eco-certification.
    /// </summary>
    [StringLength(100, ErrorMessage = "Country of origin cannot exceed 100 characters.")]
    [Display(Name = "Country of Origin", Description = "Country of origin for eco-certification.")]
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// Carbon footprint value (in kg CO2 equivalent).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Carbon footprint must be non-negative.")]
    [Display(Name = "Carbon Footprint", Description = "Carbon footprint value (in kg CO2 equivalent).")]
    public decimal? CarbonFootprintKg { get; set; }

    /// <summary>
    /// Water usage in production (in liters).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Water usage must be non-negative.")]
    [Display(Name = "Water Usage", Description = "Water usage in production (in liters).")]
    public decimal? WaterUsageLiters { get; set; }

    /// <summary>
    /// Energy consumption in production (in kWh).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Energy consumption must be non-negative.")]
    [Display(Name = "Energy Consumption", Description = "Energy consumption in production (in kWh).")]
    public decimal? EnergyConsumptionKwh { get; set; }

    /// <summary>
    /// Percentage of recycled materials used.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Recycled content percentage must be between 0 and 100.")]
    [Display(Name = "Recycled Content %", Description = "Percentage of recycled materials used.")]
    public decimal? RecycledContentPercentage { get; set; }

    /// <summary>
    /// Indicates if the product/lot is recyclable.
    /// </summary>
    [Display(Name = "Recyclable", Description = "Indicates if the product/lot is recyclable.")]
    public bool IsRecyclable { get; set; } = false;

    /// <summary>
    /// Indicates if the product/lot is biodegradable.
    /// </summary>
    [Display(Name = "Biodegradable", Description = "Indicates if the product/lot is biodegradable.")]
    public bool IsBiodegradable { get; set; } = false;

    /// <summary>
    /// Indicates if organic certification is present.
    /// </summary>
    [Display(Name = "Organic", Description = "Indicates if organic certification is present.")]
    public bool IsOrganic { get; set; } = false;

    /// <summary>
    /// Indicates if fair trade certified.
    /// </summary>
    [Display(Name = "Fair Trade", Description = "Indicates if fair trade certified.")]
    public bool IsFairTrade { get; set; } = false;

    /// <summary>
    /// Additional notes about the certification.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the certification.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Reference to the certificate document.
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference to the certificate document.")]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Verification status of the certificate.
    /// </summary>
    [Display(Name = "Verified", Description = "Verification status of the certificate.")]
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Date when the certificate was verified.
    /// </summary>
    [Display(Name = "Verification Date", Description = "Date when the certificate was verified.")]
    public DateTime? VerificationDate { get; set; }

    /// <summary>
    /// Person who verified the certificate.
    /// </summary>
    [StringLength(100, ErrorMessage = "Verified by cannot exceed 100 characters.")]
    [Display(Name = "Verified By", Description = "Person who verified the certificate.")]
    public string? VerifiedBy { get; set; }
}

/// <summary>
/// Types of sustainability certificates.
/// </summary>
public enum SustainabilityCertificateType
{
    ISO14001,           // Environmental Management System
    ISO50001,           // Energy Management System
    LEED,              // Leadership in Energy and Environmental Design
    CarbonNeutral,     // Carbon Neutral Certification
    OrganicCertification, // Organic product certification
    FairTrade,         // Fair Trade certification
    FSC,               // Forest Stewardship Council
    Cradle2Cradle,     // Cradle to Cradle certification
    EUEcolabel,        // EU Ecolabel
    EnergyStarRated,   // Energy Star certification
    GreenSeal,         // Green Seal certification
    B_Corporation,     // B Corp certification
    RainforestAlliance, // Rainforest Alliance certification
    SustainableForestry, // Sustainable forestry certification
    RecycledContent,   // Recycled content certification
    Biodegradable,     // Biodegradability certification
    ZeroWaste,         // Zero waste certification
    WaterStewardship,  // Water stewardship certification
    AnimalWelfare,     // Animal welfare certification
    Custom             // Custom certification type
}

/// <summary>
/// Status of sustainability certificates.
/// </summary>
public enum CertificateStatus
{
    Valid,             // Certificate is currently valid
    Expired,           // Certificate has expired
    Pending,           // Certificate is pending approval
    Suspended,         // Certificate has been suspended
    Revoked,           // Certificate has been revoked
    UnderReview        // Certificate is under review
}
