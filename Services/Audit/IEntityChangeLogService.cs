/// <summary>
/// Interface for managing entity change log operations.
/// </summary>
public interface IEntityChangeLogService
{
    /// <summary>
    /// Retrieves the change history for a specific entity with optional filtering.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="operationType">Optional operation type filter (Insert, Update, Delete)</param>
    /// <param name="changedBy">Optional user filter</param>
    /// <param name="propertyName">Optional property name filter</param>
    /// <returns>List of EntityChangeLog entries</returns>
    Task<List<EntityChangeLog>> GetEntityHistoryAsync(
        string entityName,
        Guid entityId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        string? changedBy = null,
        string? propertyName = null);

    /// <summary>
    /// Adds a new change log entry for an entity modification.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="operationType">Type of operation (Insert, Update, Delete)</param>
    /// <param name="propertyName">Name of the changed property</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    /// <param name="changedBy">User who made the change</param>
    /// <param name="entityDisplayName">Optional display name for the entity</param>
    /// <returns>The created EntityChangeLog entry</returns>
    Task<EntityChangeLog> AddChangeLogAsync(
        string entityName,
        Guid entityId,
        string operationType,
        string propertyName,
        string? oldValue,
        string? newValue,
        string changedBy,
        string? entityDisplayName = null);

    /// <summary>
    /// Deletes all change history for a specific entity.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="entityId">ID of the entity</param>
    /// <returns>Number of deleted entries</returns>
    Task<int> DeleteEntityHistoryAsync(string entityName, Guid entityId);

    /// <summary>
    /// Deletes all change history for all entities (truncate).
    /// </summary>
    /// <returns>Number of deleted entries</returns>
    Task<int> DeleteAllHistoryAsync();

    /// <summary>
    /// Retrieves the last change made to a specific entity.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="entityId">ID of the entity</param>
    /// <returns>The most recent EntityChangeLog entry or null</returns>
    Task<EntityChangeLog?> GetLastChangeAsync(string entityName, Guid entityId);

    /// <summary>
    /// Retrieves the change history for all entities of a specific type.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <param name="changedBy">Optional user filter</param>
    /// <returns>List of EntityChangeLog entries</returns>
    Task<List<EntityChangeLog>> GetEntityTypeHistoryAsync(
        string entityName,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        string? changedBy = null);

    /// <summary>
    /// Retrieves all changes made by a specific user.
    /// </summary>
    /// <param name="changedBy">User who made the changes</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="entityName">Optional entity name filter</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <returns>List of EntityChangeLog entries</returns>
    Task<List<EntityChangeLog>> GetUserChangesAsync(
        string changedBy,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? entityName = null,
        string? operationType = null);

    /// <summary>
    /// Retrieves all changes made to a specific property of an entity.
    /// </summary>
    /// <param name="entityName">Name of the entity class</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="propertyName">Name of the property</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="changedBy">Optional user filter</param>
    /// <returns>List of EntityChangeLog entries</returns>
    Task<List<EntityChangeLog>> GetPropertyChangesAsync(
        string entityName,
        Guid entityId,
        string propertyName,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? changedBy = null);
}