using EventForge.DTOs.Documents;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Interface for document row validation
/// </summary>
public interface IDocumentRowValidator
{
    /// <summary>
    /// Validates a create document row DTO
    /// </summary>
    ValidationResult Validate(CreateDocumentRowDto model);

    /// <summary>
    /// Validates an update document row DTO
    /// </summary>
    ValidationResult Validate(UpdateDocumentRowDto model);
}
