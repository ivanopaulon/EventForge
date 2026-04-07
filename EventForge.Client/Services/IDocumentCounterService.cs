using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Interface for document counter management service.
/// </summary>
public interface IDocumentCounterService
{
    /// <summary>
    /// Gets all document counters.
    /// </summary>
    Task<IEnumerable<DocumentCounterDto>?> GetAllDocumentCountersAsync();

    /// <summary>
    /// Gets all document counters for a specific document type.
    /// </summary>
    Task<IEnumerable<DocumentCounterDto>?> GetDocumentCountersByTypeAsync(Guid documentTypeId);

    /// <summary>
    /// Gets a document counter by ID.
    /// </summary>
    Task<DocumentCounterDto?> GetDocumentCounterByIdAsync(Guid id);

    /// <summary>
    /// Creates a new document counter.
    /// </summary>
    Task<DocumentCounterDto?> CreateDocumentCounterAsync(CreateDocumentCounterDto createDto);

    /// <summary>
    /// Updates an existing document counter.
    /// </summary>
    Task<DocumentCounterDto?> UpdateDocumentCounterAsync(Guid id, UpdateDocumentCounterDto updateDto);

    /// <summary>
    /// Deletes a document counter.
    /// </summary>
    Task<bool> DeleteDocumentCounterAsync(Guid id);
}
