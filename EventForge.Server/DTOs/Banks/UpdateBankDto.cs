using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Banks;

/// <summary>
/// DTO for Bank update operations.
/// Contains only fields that can be modified after creation.
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

    // Note: Removed Code and SwiftBic - these are regulatory identifiers that shouldn't change
}