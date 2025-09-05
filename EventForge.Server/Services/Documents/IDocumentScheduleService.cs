using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document schedules
/// </summary>
public interface IDocumentScheduleService
{
    /// <summary>
    /// Gets all schedules for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document schedules</returns>
    Task<IEnumerable<DocumentScheduleDto>> GetDocumentSchedulesAsync(Guid documentHeaderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules by document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document schedules</returns>
    Task<IEnumerable<DocumentScheduleDto>> GetDocumentTypeSchedulesAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific document schedule by ID
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document schedule details</returns>
    Task<DocumentScheduleDto?> GetDocumentScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document schedule
    /// </summary>
    /// <param name="createDto">Schedule creation data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document schedule</returns>
    Task<DocumentScheduleDto> CreateDocumentScheduleAsync(CreateDocumentScheduleDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document schedule
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="updateDto">Schedule update data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document schedule</returns>
    Task<DocumentScheduleDto?> UpdateDocumentScheduleAsync(Guid scheduleId, UpdateDocumentScheduleDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document schedule
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentScheduleAsync(Guid scheduleId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all schedules that need to be executed
    /// </summary>
    /// <param name="beforeDate">Get schedules with execution date before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules ready for execution</returns>
    Task<IEnumerable<DocumentScheduleDto>> GetSchedulesForExecutionAsync(DateTime? beforeDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a schedule and updates the next execution date
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document schedule</returns>
    Task<DocumentScheduleDto?> ExecuteScheduleAsync(Guid scheduleId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the next execution date for a schedule
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="fromDate">Calculate from this date (optional, uses current date if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next execution date</returns>
    Task<DateTime?> CalculateNextExecutionDateAsync(Guid scheduleId, DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules by category
    /// </summary>
    /// <param name="category">Schedule category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules in the category</returns>
    Task<IEnumerable<DocumentScheduleDto>> GetSchedulesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses or resumes a schedule
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <param name="pause">True to pause, false to resume</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document schedule</returns>
    Task<DocumentScheduleDto?> ToggleScheduleAsync(Guid scheduleId, bool pause, string currentUser, CancellationToken cancellationToken = default);
}