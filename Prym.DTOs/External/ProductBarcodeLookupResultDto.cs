namespace Prym.DTOs.External;

/// <summary>
/// DTO for product barcode lookup results.
/// </summary>
public class ProductBarcodeLookupResultDto
{
    /// <summary>
    /// Indicates whether product data was found.
    /// </summary>
    public bool IsFound { get; set; }

    /// <summary>
    /// Barcode/code used for lookup.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Brand name.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Short description.
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Product image URL from external provider.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Source service name that provided the data.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Error details for failed lookups.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
