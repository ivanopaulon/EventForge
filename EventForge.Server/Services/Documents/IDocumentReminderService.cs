using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document reminders
/// </summary>
public interface IDocumentReminderService
{
    /// <summary>
    /// Gets all reminders for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document reminders</returns>
    Task<IEnumerable<DocumentReminderDto>> GetDocumentRemindersAsync(Guid documentHeaderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific document reminder by ID
    /// </summary>
    /// <param name="reminderId">Reminder ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document reminder details</returns>
    Task<DocumentReminderDto?> GetDocumentReminderAsync(Guid reminderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document reminder
    /// </summary>
    /// <param name="createDto">Reminder creation data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document reminder</returns>
    Task<DocumentReminderDto> CreateDocumentReminderAsync(CreateDocumentReminderDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document reminder
    /// </summary>
    /// <param name="reminderId">Reminder ID</param>
    /// <param name="updateDto">Reminder update data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document reminder</returns>
    Task<DocumentReminderDto?> UpdateDocumentReminderAsync(Guid reminderId, UpdateDocumentReminderDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document reminder
    /// </summary>
    /// <param name="reminderId">Reminder ID</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentReminderAsync(Guid reminderId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a reminder as completed
    /// </summary>
    /// <param name="reminderId">Reminder ID</param>
    /// <param name="completionNotes">Completion notes</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document reminder</returns>
    Task<DocumentReminderDto?> CompleteReminderAsync(Guid reminderId, string? completionNotes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Snoozes a reminder to a later date
    /// </summary>
    /// <param name="reminderId">Reminder ID</param>
    /// <param name="newTargetDate">New target date</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document reminder</returns>
    Task<DocumentReminderDto?> SnoozeReminderAsync(Guid reminderId, DateTime newTargetDate, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active reminders that need to be processed
    /// </summary>
    /// <param name="beforeDate">Get reminders with target date before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active reminders</returns>
    Task<IEnumerable<DocumentReminderDto>> GetActiveRemindersAsync(DateTime? beforeDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reminders by user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="includeCompleted">Include completed reminders</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user reminders</returns>
    Task<IEnumerable<DocumentReminderDto>> GetUserRemindersAsync(string userId, bool includeCompleted = false, CancellationToken cancellationToken = default);
}