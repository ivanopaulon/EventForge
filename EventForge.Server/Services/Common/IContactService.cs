namespace EventForge.Server.Services.Common;

/// <summary>
/// Service interface for managing contacts.
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Gets all contacts with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contacts</returns>
    Task<PagedResult<ContactDto>> GetContactsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets contacts by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of contacts for the owner</returns>
    Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contact by ID.
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contact DTO or null if not found</returns>
    Task<ContactDto?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new contact.
    /// </summary>
    /// <param name="createContactDto">Contact creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created contact DTO</returns>
    Task<ContactDto> CreateContactAsync(CreateContactDto createContactDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="updateContactDto">Contact update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated contact DTO or null if not found</returns>
    Task<ContactDto?> UpdateContactAsync(Guid id, UpdateContactDto updateContactDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a contact (soft delete).
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteContactAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a contact exists.
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ContactExistsAsync(Guid contactId, CancellationToken cancellationToken = default);
}