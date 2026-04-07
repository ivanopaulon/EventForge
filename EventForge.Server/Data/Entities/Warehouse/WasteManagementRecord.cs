using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents waste management and disposal records for products and materials.
/// </summary>
public class WasteManagementRecord : AuditableEntity
{
    /// <summary>
    /// Product this waste record is for (if applicable).
    /// </summary>
    [Display(Name = "Product", Description = "Product this waste record is for.")]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot this waste record is for (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot this waste record is for.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Serial this waste record is for (if applicable).
    /// </summary>
    [Display(Name = "Serial", Description = "Serial this waste record is for.")]
    public Guid? SerialId { get; set; }

    /// <summary>
    /// Navigation property for the serial.
    /// </summary>
    public Serial? Serial { get; set; }

    /// <summary>
    /// Storage location where waste was generated.
    /// </summary>
    [Display(Name = "Location", Description = "Storage location where waste was generated.")]
    public Guid? StorageLocationId { get; set; }

    /// <summary>
    /// Navigation property for the storage location.
    /// </summary>
    public StorageLocation? StorageLocation { get; set; }

    /// <summary>
    /// Waste record number.
    /// </summary>
    [Required(ErrorMessage = "Record number is required.")]
    [StringLength(50, ErrorMessage = "Record number cannot exceed 50 characters.")]
    [Display(Name = "Record Number", Description = "Waste record number.")]
    public string RecordNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of waste.
    /// </summary>
    [Required(ErrorMessage = "Waste type is required.")]
    [Display(Name = "Waste Type", Description = "Type of waste.")]
    public WasteType WasteType { get; set; }

    /// <summary>
    /// Reason for waste/disposal.
    /// </summary>
    [Required(ErrorMessage = "Waste reason is required.")]
    [Display(Name = "Waste Reason", Description = "Reason for waste/disposal.")]
    public WasteReason Reason { get; set; }

    /// <summary>
    /// Quantity of waste (in product's unit of measure).
    /// </summary>
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    [Display(Name = "Quantity", Description = "Quantity of waste.")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Weight of waste in kilograms.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Weight must be non-negative.")]
    [Display(Name = "Weight (kg)", Description = "Weight of waste in kilograms.")]
    public decimal? WeightKg { get; set; }

    /// <summary>
    /// Date when waste was generated.
    /// </summary>
    [Required(ErrorMessage = "Waste date is required.")]
    [Display(Name = "Waste Date", Description = "Date when waste was generated.")]
    public DateTime WasteDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Disposal method used.
    /// </summary>
    [Required(ErrorMessage = "Disposal method is required.")]
    [Display(Name = "Disposal Method", Description = "Disposal method used.")]
    public DisposalMethod DisposalMethod { get; set; }

    /// <summary>
    /// Date when waste was disposed.
    /// </summary>
    [Display(Name = "Disposal Date", Description = "Date when waste was disposed.")]
    public DateTime? DisposalDate { get; set; }

    /// <summary>
    /// Company or facility that handled disposal.
    /// </summary>
    [StringLength(200, ErrorMessage = "Disposal company cannot exceed 200 characters.")]
    [Display(Name = "Disposal Company", Description = "Company or facility that handled disposal.")]
    public string? DisposalCompany { get; set; }

    /// <summary>
    /// Cost of waste disposal.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Cost must be non-negative.")]
    [Display(Name = "Disposal Cost", Description = "Cost of waste disposal.")]
    public decimal? DisposalCost { get; set; }

    /// <summary>
    /// Status of the waste disposal.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the waste disposal.")]
    public WasteDisposalStatus Status { get; set; } = WasteDisposalStatus.Pending;

    /// <summary>
    /// Indicates if hazardous waste.
    /// </summary>
    [Display(Name = "Hazardous", Description = "Indicates if hazardous waste.")]
    public bool IsHazardous { get; set; } = false;

    /// <summary>
    /// Hazard classification code (if applicable).
    /// </summary>
    [StringLength(50, ErrorMessage = "Hazard code cannot exceed 50 characters.")]
    [Display(Name = "Hazard Code", Description = "Hazard classification code.")]
    public string? HazardCode { get; set; }

    /// <summary>
    /// Environmental impact assessment.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Environmental impact cannot exceed 1000 characters.")]
    [Display(Name = "Environmental Impact", Description = "Environmental impact assessment.")]
    public string? EnvironmentalImpact { get; set; }

    /// <summary>
    /// Recycling rate achieved (percentage).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Recycling rate must be between 0 and 100.")]
    [Display(Name = "Recycling Rate %", Description = "Recycling rate achieved (percentage).")]
    public decimal? RecyclingRatePercentage { get; set; }

    /// <summary>
    /// Material recovery value (if applicable).
    /// </summary>
    [Display(Name = "Recovery Value", Description = "Material recovery value.")]
    public decimal? RecoveryValue { get; set; }

    /// <summary>
    /// Person responsible for waste management.
    /// </summary>
    [StringLength(100, ErrorMessage = "Responsible person cannot exceed 100 characters.")]
    [Display(Name = "Responsible Person", Description = "Person responsible for waste management.")]
    public string? ResponsiblePerson { get; set; }

    /// <summary>
    /// Notes and additional details.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Notes and additional details.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Reference to disposal certificate or documentation.
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference to disposal certificate.")]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Certificate number for waste disposal (if required by regulations).
    /// </summary>
    [StringLength(100, ErrorMessage = "Certificate number cannot exceed 100 characters.")]
    [Display(Name = "Certificate Number", Description = "Certificate number for waste disposal.")]
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Regulatory compliance status.
    /// </summary>
    [Display(Name = "Compliant", Description = "Regulatory compliance status.")]
    public bool IsCompliant { get; set; } = true;
}

/// <summary>
/// Types of waste.
/// </summary>
public enum WasteType
{
    ProductDefect,      // Defective products
    Expired,            // Expired products
    Damaged,            // Damaged items
    Packaging,          // Packaging materials
    RawMaterial,        // Raw materials
    ProductionWaste,    // Production waste
    ReturnedGoods,      // Returned goods (unsellable)
    Obsolete,           // Obsolete inventory
    Scrap,              // Scrap materials
    Hazardous,          // Hazardous waste
    Electronic,         // Electronic waste (WEEE)
    Chemical,           // Chemical waste
    Organic,            // Organic waste
    Plastic,            // Plastic waste
    Paper,              // Paper waste
    Metal,              // Metal waste
    Glass,              // Glass waste
    Mixed               // Mixed waste
}

/// <summary>
/// Reasons for waste generation.
/// </summary>
public enum WasteReason
{
    QualityIssue,       // Quality control failure
    Expiration,         // Product expired
    Damage,             // Physical damage
    CustomerReturn,     // Customer return (unsellable)
    OverProduction,     // Overproduction
    ProcessDefect,      // Manufacturing defect
    StorageDamage,      // Damage during storage
    TransportDamage,    // Damage during transport
    Obsolescence,       // Product obsolescence
    RawMaterialWaste,   // Raw material waste
    PackagingDefect,    // Packaging defect
    Recall,             // Product recall
    Contamination,      // Contamination
    TechnicalFailure,   // Technical failure
    Other               // Other reason
}

/// <summary>
/// Methods for waste disposal.
/// </summary>
public enum DisposalMethod
{
    Recycling,          // Recycling
    Composting,         // Composting (organic waste)
    Incineration,       // Incineration with energy recovery
    Landfill,           // Landfill disposal
    Donation,           // Donation to charity
    Resale,             // Resale at discount
    MaterialRecovery,   // Material recovery/refurbishment
    ChemicalTreatment,  // Chemical treatment
    BiologicalTreatment, // Biological treatment
    Reuse,              // Direct reuse
    Repurposing,        // Repurposing for other uses
    HazardousWasteFacility, // Specialized hazardous waste facility
    AuthorizedDisposal, // Authorized third-party disposal
    ReturnToSupplier,   // Return to supplier
    Other               // Other disposal method
}

/// <summary>
/// Status of waste disposal process.
/// </summary>
public enum WasteDisposalStatus
{
    Pending,            // Waste generated, pending disposal
    InProgress,         // Disposal in progress
    Completed,          // Disposal completed
    Cancelled,          // Disposal cancelled
    OnHold,             // On hold for further investigation
    AwaitingPickup,     // Awaiting pickup by disposal company
    AwaitingApproval    // Awaiting regulatory approval
}
