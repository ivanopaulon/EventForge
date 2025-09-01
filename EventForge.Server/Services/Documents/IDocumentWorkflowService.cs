using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document workflows
/// </summary>
public interface IDocumentWorkflowService
{
    /// <summary>
    /// Gets all document workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflow DTOs</returns>
    Task<IEnumerable<DocumentWorkflowDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document workflow by ID
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document workflow DTO or null if not found</returns>
    Task<DocumentWorkflowDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document workflows by document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflow DTOs for the specified document type</returns>
    Task<IEnumerable<DocumentWorkflowDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active document workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active document workflow DTOs</returns>
    Task<IEnumerable<DocumentWorkflowDto>> GetActiveWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document workflows by category
    /// </summary>
    /// <param name="category">Workflow category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflow DTOs for the specified category</returns>
    Task<IEnumerable<DocumentWorkflowDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document workflow
    /// </summary>
    /// <param name="createDto">Document workflow creation data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document workflow DTO</returns>
    Task<DocumentWorkflowDto> CreateAsync(CreateDocumentWorkflowDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document workflow
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="updateDto">Document workflow update data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document workflow DTO or null if not found</returns>
    Task<DocumentWorkflowDto?> UpdateAsync(Guid id, UpdateDocumentWorkflowDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document workflow (soft delete)
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a document workflow
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="isActive">Whether to activate or deactivate the workflow</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version of a workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest version number or null if workflow not found</returns>
    Task<int?> GetLatestVersionAsync(Guid workflowId, CancellationToken cancellationToken = default);
}