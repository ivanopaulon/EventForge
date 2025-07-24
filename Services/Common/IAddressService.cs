using EventForge.Models.Common;

namespace EventForge.Services.Common;

/// <summary>
/// Service interface for managing addresses.
/// </summary>
public interface IAddressService
{
    /// <summary>
    /// Gets all addresses with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of addresses</returns>
    Task<PagedResult<AddressDto>> GetAddressesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets addresses by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses for the owner</returns>
    Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an address by ID.
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Address DTO or null if not found</returns>
    Task<AddressDto?> GetAddressByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name="createAddressDto">Address creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created address DTO</returns>
    Task<AddressDto> CreateAddressAsync(CreateAddressDto createAddressDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="updateAddressDto">Address update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated address DTO or null if not found</returns>
    Task<AddressDto?> UpdateAddressAsync(Guid id, UpdateAddressDto updateAddressDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an address (soft delete).
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAddressAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an address exists.
    /// </summary>
    /// <param name="addressId">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> AddressExistsAsync(Guid addressId, CancellationToken cancellationToken = default);
}