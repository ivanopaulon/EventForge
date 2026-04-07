namespace EventForge.Server.Services.Common;

/// <summary>
/// Service interface for managing classification nodes in a hierarchical structure.
/// </summary>
public interface IClassificationNodeService
{
    /// <summary>
    /// Gets all classification nodes with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="parentId">Optional parent ID to filter children</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of classification nodes</returns>
    Task<PagedResult<ClassificationNodeDto>> GetClassificationNodesAsync(PaginationParameters pagination, Guid? parentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a classification node by ID.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Classification node or null if not found</returns>
    Task<ClassificationNodeDto?> GetClassificationNodeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the root classification nodes (nodes without parent).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root classification nodes</returns>
    Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all children of a specific classification node.
    /// </summary>
    /// <param name="parentId">Parent classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child classification nodes</returns>
    Task<IEnumerable<ClassificationNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new classification node.
    /// </summary>
    /// <param name="createDto">Classification node creation data</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created classification node</returns>
    Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing classification node.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="updateDto">Classification node update data</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated classification node or null if not found</returns>
    Task<ClassificationNodeDto?> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a classification node.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="rowVersion">Row version for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteClassificationNodeAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default);
}