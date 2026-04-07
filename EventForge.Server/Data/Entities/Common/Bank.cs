using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Common;


/// <summary>
/// Represents a bank entity.
/// </summary>
public class Bank : AuditableEntity
{
    /// <summary>
    /// Name of the bank.
    /// </summary>
    [Required(ErrorMessage = "The bank name is required.")]
    [MaxLength(100, ErrorMessage = "The bank name cannot exceed 100 characters.")]
    [Display(Name = "Bank Name", Description = "Name of the bank.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bank code (e.g., ABI, SWIFT/BIC).
    /// </summary>
    [MaxLength(20, ErrorMessage = "The bank code cannot exceed 20 characters.")]
    [Display(Name = "Bank Code", Description = "Bank code (ABI, SWIFT/BIC, etc.).")]
    public string? Code { get; set; } = string.Empty;

    /// <summary>
    /// SWIFT/BIC code.
    /// </summary>
    [MaxLength(20, ErrorMessage = "The SWIFT/BIC code cannot exceed 20 characters.")]
    [Display(Name = "SWIFT/BIC", Description = "SWIFT/BIC code of the bank.")]
    public string? SwiftBic { get; set; } = string.Empty;

    /// <summary>
    /// Bank branch or agency.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The branch name cannot exceed 100 characters.")]
    [Display(Name = "Branch", Description = "Bank branch or agency.")]
    public string? Branch { get; set; } = string.Empty;

    /// <summary>
    /// Bank address.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The address cannot exceed 200 characters.")]
    [Display(Name = "Address", Description = "Bank address.")]
    public string? Address { get; set; } = string.Empty;

    /// <summary>
    /// Country where the bank is located.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The country name cannot exceed 50 characters.")]
    [Display(Name = "Country", Description = "Country where the bank is located.")]
    public string? Country { get; set; } = string.Empty;

    /// <summary>
    /// Bank phone number.
    /// </summary>
    [MaxLength(30, ErrorMessage = "The phone number cannot exceed 30 characters.")]
    [Display(Name = "Phone", Description = "Bank phone number.")]
    public string? Phone { get; set; } = string.Empty;

    /// <summary>
    /// Bank email address.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [Display(Name = "Email", Description = "Bank email address.")]
    public string? Email { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;

    /// <summary>
    /// Addresses associated with the bank.
    /// </summary>
    [Display(Name = "Addresses", Description = "Addresses associated with the bank.")]
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    /// <summary>
    /// Contacts associated with the bank.
    /// </summary>
    [Display(Name = "Contacts", Description = "Contacts associated with the bank.")]
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    /// <summary>
    /// Reference persons associated with the bank.
    /// </summary>
    [Display(Name = "References", Description = "Reference persons associated with the bank.")]
    public ICollection<Reference> References { get; set; } = new List<Reference>();
}