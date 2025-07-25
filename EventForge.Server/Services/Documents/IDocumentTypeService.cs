using EventForge.Server.DTOs.Documents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document types
/// </summary>
public interface IDocumentTypeService
{
    /// <summary>
    /// Gets all document types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document type DTOs</returns>
    Task<IEnumerable<DocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document type by ID
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document type DTO or null if not found</returns>
    Task<DocumentTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document type
    /// </summary>
    /// <param name="createDto">Document type creation data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document type DTO</returns>
    Task<DocumentTypeDto> CreateAsync(CreateDocumentTypeDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document type
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="updateDto">Document type update data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document type DTO or null if not found</returns>
    Task<DocumentTypeDto?> UpdateAsync(Guid id, UpdateDocumentTypeDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document type (soft delete)
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}