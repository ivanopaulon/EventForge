using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document recurrence
/// </summary>
public interface IDocumentRecurrenceService
{
    /// <summary>
    /// Gets all document recurrence schedules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document recurrence DTOs</returns>
    Task<IEnumerable<DocumentRecurrenceDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document recurrence by ID
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document recurrence DTO or null if not found</returns>
    Task<DocumentRecurrenceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document recurrences by template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document recurrence DTOs for the specified template</returns>
    Task<IEnumerable<DocumentRecurrenceDto>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active document recurrence schedules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active document recurrence DTOs</returns>
    Task<IEnumerable<DocumentRecurrenceDto>> GetActiveSchedulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document recurrences due for execution
    /// </summary>
    /// <param name="upToDate">Maximum date to check for due schedules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document recurrence DTOs due for execution</returns>
    Task<IEnumerable<DocumentRecurrenceDto>> GetDueForExecutionAsync(DateTime upToDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document recurrence
    /// </summary>
    /// <param name="createDto">Document recurrence creation data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document recurrence DTO</returns>
    Task<DocumentRecurrenceDto> CreateAsync(CreateDocumentRecurrenceDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document recurrence
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="updateDto">Document recurrence update data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document recurrence DTO or null if not found</returns>
    Task<DocumentRecurrenceDto?> UpdateAsync(Guid id, UpdateDocumentRecurrenceDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document recurrence (soft delete)
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a document recurrence schedule
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="isEnabled">Whether to enable or disable the schedule</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    Task<bool> SetEnabledStatusAsync(Guid id, bool isEnabled, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the execution tracking for a recurrence schedule
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="executionDate">Date of execution</param>
    /// <param name="success">Whether the execution was successful</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateExecutionTrackingAsync(Guid id, DateTime executionDate, bool success, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the next execution date for a recurrence schedule
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next execution date or null if recurrence is complete or disabled</returns>
    Task<DateTime?> CalculateNextExecutionDateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recurrence schedules by status
    /// </summary>
    /// <param name="status">Recurrence status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document recurrence DTOs with the specified status</returns>
    Task<IEnumerable<DocumentRecurrenceDto>> GetByStatusAsync(RecurrenceStatus status, CancellationToken cancellationToken = default);
}