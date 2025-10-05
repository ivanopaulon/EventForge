using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Common;


/// <summary>
/// Represents a VAT nature (Natura IVA) entity for Italian tax compliance.
/// VAT nature codes are used to indicate the reason for non-taxation or special VAT treatment.
/// </summary>
public class VatNature : AuditableEntity
{
    /// <summary>
    /// Code of the VAT nature (e.g., "N1", "N2", "N3", etc.).
    /// </summary>
    [Required(ErrorMessage = "The code is required.")]
    [MaxLength(10, ErrorMessage = "The code cannot exceed 10 characters.")]
    [Display(Name = "Code", Description = "Code of the VAT nature (e.g., N1, N2).")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Name of the VAT nature.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the VAT nature.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the VAT nature explaining its purpose and usage.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the VAT nature.")]
    public string? Description { get; set; }

    /// <summary>
    /// VAT rates associated with this nature.
    /// </summary>
    [Display(Name = "VAT Rates", Description = "VAT rates associated with this nature.")]
    public ICollection<VatRate> VatRates { get; set; } = new List<VatRate>();
}
