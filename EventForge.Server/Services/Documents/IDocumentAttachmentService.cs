using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document attachments
/// </summary>
public interface IDocumentAttachmentService
{
    /// <summary>
    /// Gets all attachments for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    Task<IEnumerable<DocumentAttachmentDto>> GetDocumentHeaderAttachmentsAsync(
        Guid documentHeaderId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all attachments for a document row
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    Task<IEnumerable<DocumentAttachmentDto>> GetDocumentRowAttachmentsAsync(
        Guid documentRowId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document attachment by ID
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document attachment DTO or null if not found</returns>
    Task<DocumentAttachmentDto?> GetAttachmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document attachment
    /// </summary>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created attachment DTO</returns>
    Task<DocumentAttachmentDto> CreateAttachmentAsync(
        CreateDocumentAttachmentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document attachment metadata
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="updateDto">Attachment update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated attachment DTO or null if not found</returns>
    Task<DocumentAttachmentDto?> UpdateAttachmentAsync(
        Guid id,
        UpdateDocumentAttachmentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing attachment
    /// </summary>
    /// <param name="id">Original attachment ID</param>
    /// <param name="versionDto">New version data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New version attachment DTO or null if original not found</returns>
    Task<DocumentAttachmentDto?> CreateAttachmentVersionAsync(
        Guid id,
        AttachmentVersionDto versionDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document attachment (soft delete)
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAttachmentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attachment version history
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachment versions</returns>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs an attachment digitally
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="signatureInfo">Digital signature information</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated attachment DTO or null if not found</returns>
    Task<DocumentAttachmentDto?> SignAttachmentAsync(
        Guid id,
        string signatureInfo,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attachments by category
    /// </summary>
    /// <param name="category">Attachment category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachments in the category</returns>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an attachment exists
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> AttachmentExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}