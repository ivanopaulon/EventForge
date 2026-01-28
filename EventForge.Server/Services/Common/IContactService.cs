namespace EventForge.Server.Services.Common;

/// <summary>
/// Service interface for managing contacts.
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Gets all contacts with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contacts</returns>
    Task<PagedResult<ContactDto>> GetContactsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets contacts by owner and purpose.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="ownerType">Owner type</param>
    /// <param name="purpose">Contact purpose</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of contacts matching the criteria</returns>
    Task<IEnumerable<ContactDto>> GetContactsByOwnerAndPurposeAsync(Guid ownerId, string ownerType, ContactPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary contact of a specific type for an owner.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="ownerType">Owner type</param>
    /// <param name="contactType">Contact type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Primary contact or null if not found</returns>
    Task<ContactDto?> GetPrimaryContactAsync(Guid ownerId, string ownerType, ContactType contactType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates emergency contact requirements for minors.
    /// </summary>
    /// <param name="ownerId">Owner ID (TeamMember ID)</param>
    /// <param name="isMinor">Whether the team member is a minor</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if emergency contact requirements are met, false otherwise</returns>
    Task<bool> ValidateEmergencyContactRequirementsAsync(Guid ownerId, bool isMinor, CancellationToken cancellationToken = default);
}