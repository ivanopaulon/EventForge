using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Store;

/// <summary>
/// Request DTO for setting or validating a store user quick PIN.
/// </summary>
public class StoreUserPinDto
{
    /// <summary>
    /// Regular expression pattern shared by client and server validation: 4 to 6 numeric digits.
    /// </summary>
    public const string PinPattern = @"^\d{4,6}$";

    /// <summary>
    /// Numeric quick PIN (4-6 digits).
    /// </summary>
    [Required(ErrorMessage = "Il PIN è obbligatorio.")]
    [RegularExpression(PinPattern, ErrorMessage = "Il PIN deve contenere da 4 a 6 cifre.")]
    public string Pin { get; set; } = string.Empty;
}

