using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document comments and collaboration
/// </summary>
public interface IDocumentCommentService
{
    /// <summary>
    /// Gets all comments for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    Task<IEnumerable<DocumentCommentDto>> GetDocumentHeaderCommentsAsync(
        Guid documentHeaderId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comments for a document row
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    Task<IEnumerable<DocumentCommentDto>> GetDocumentRowCommentsAsync(
        Guid documentRowId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document comment by ID
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document comment DTO or null if not found</returns>
    Task<DocumentCommentDto?> GetCommentByIdAsync(
        Guid id,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document comment
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created comment DTO</returns>
    Task<DocumentCommentDto> CreateCommentAsync(
        CreateDocumentCommentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document comment
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="updateDto">Comment update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    Task<DocumentCommentDto?> UpdateCommentAsync(
        Guid id,
        UpdateDocumentCommentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document comment (soft delete)
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteCommentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a comment
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="resolveDto">Resolution data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    Task<DocumentCommentDto?> ResolveCommentAsync(
        Guid id,
        ResolveCommentDto resolveDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens a resolved comment
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    Task<DocumentCommentDto?> ReopenCommentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments assigned to a specific user
    /// </summary>
    /// <param name="username">Username to get assignments for</param>
    /// <param name="statusFilter">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of assigned comments</returns>
    Task<IEnumerable<DocumentCommentDto>> GetAssignedCommentsAsync(
        string username,
        string? statusFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments by priority level
    /// </summary>
    /// <param name="priority">Priority level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of comments with specified priority</returns>
    Task<IEnumerable<DocumentCommentDto>> GetCommentsByPriorityAsync(
        string priority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments by status
    /// </summary>
    /// <param name="status">Comment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of comments with specified status</returns>
    Task<IEnumerable<DocumentCommentDto>> GetCommentsByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments by type
    /// </summary>
    /// <param name="commentType">Comment type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of comments with specified type</returns>
    Task<IEnumerable<DocumentCommentDto>> GetCommentsByTypeAsync(
        string commentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comment statistics for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="currentUser">Current user for filtering assigned items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comment statistics</returns>
    Task<DocumentCommentStatsDto> GetDocumentCommentStatsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replies to a specific comment
    /// </summary>
    /// <param name="parentCommentId">Parent comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of reply comments</returns>
    Task<IEnumerable<DocumentCommentDto>> GetCommentRepliesAsync(
        Guid parentCommentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a comment to a user
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="assignToUsername">Username to assign to</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    Task<DocumentCommentDto?> AssignCommentAsync(
        Guid id,
        string assignToUsername,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches comments with filters
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="documentHeaderId">Optional document header filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="priority">Optional priority filter</param>
    /// <param name="assignedTo">Optional assigned user filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching comments</returns>
    Task<IEnumerable<DocumentCommentDto>> SearchCommentsAsync(
        string searchText,
        Guid? documentHeaderId = null,
        string? status = null,
        string? priority = null,
        string? assignedTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a comment exists
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> CommentExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}