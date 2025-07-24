using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.Banks;

/// <summary>
/// DTO for Bank update operations.
/// </summary>
public class UpdateBankDto
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
    public string? Code { get; set; }

    /// <summary>
    /// SWIFT/BIC code.
    /// </summary>
    [MaxLength(20, ErrorMessage = "The SWIFT/BIC code cannot exceed 20 characters.")]
    [Display(Name = "SWIFT/BIC", Description = "SWIFT/BIC code of the bank.")]
    public string? SwiftBic { get; set; }

    /// <summary>
    /// Bank branch or agency.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The branch name cannot exceed 100 characters.")]
    [Display(Name = "Branch", Description = "Bank branch or agency.")]
    public string? Branch { get; set; }

    /// <summary>
    /// Bank address.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The address cannot exceed 200 characters.")]
    [Display(Name = "Address", Description = "Bank address.")]
    public string? Address { get; set; }

    /// <summary>
    /// Country where the bank is located.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The country name cannot exceed 50 characters.")]
    [Display(Name = "Country", Description = "Country where the bank is located.")]
    public string? Country { get; set; }

    /// <summary>
    /// Bank phone number.
    /// </summary>
    [MaxLength(30, ErrorMessage = "The phone number cannot exceed 30 characters.")]
    [Display(Name = "Phone", Description = "Bank phone number.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Bank email address.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [Display(Name = "Email", Description = "Bank email address.")]
    public string? Email { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; }
}