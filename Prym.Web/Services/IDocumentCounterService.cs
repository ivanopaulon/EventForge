using Prym.DTOs.Documents;

namespace Prym.Web.Services;

/// <summary>
/// Interface for document counter management service.
/// </summary>
public interface IDocumentCounterService
{
    /// <summary>
    /// Gets all document counters.
    /// </summary>
    Task<IEnumerable<DocumentCounterDto>?> GetAllDocumentCountersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all document counters for a specific document type.
    /// </summary>
    Task<IEnumerable<DocumentCounterDto>?> GetDocumentCountersByTypeAsync(Guid documentTypeId, CancellationToken ct = default);

    /// <summary>
    /// Gets a document counter by ID.
    /// </summary>
    Task<DocumentCounterDto?> GetDocumentCounterByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new document counter.
    /// </summary>
    Task<DocumentCounterDto?> CreateDocumentCounterAsync(CreateDocumentCounterDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing document counter.
    /// </summary>
    Task<DocumentCounterDto?> UpdateDocumentCounterAsync(Guid id, UpdateDocumentCounterDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a document counter.
    /// </summary>
    Task<bool> DeleteDocumentCounterAsync(Guid id, CancellationToken ct = default);
}
