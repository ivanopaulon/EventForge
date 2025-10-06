using EventForge.DTOs.VatRates;

namespace EventForge.Server.Services.VatRates;

/// <summary>
/// Service interface for managing VAT natures.
/// </summary>
public interface IVatNatureService
{
    /// <summary>
    /// Gets all VAT natures with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of VAT natures</returns>
    Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a VAT nature by ID.
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT nature DTO or null if not found</returns>
    Task<VatNatureDto?> GetVatNatureByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new VAT nature.
    /// </summary>
    /// <param name="createVatNatureDto">VAT nature creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VAT nature DTO</returns>
    Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createVatNatureDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing VAT nature.
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="updateVatNatureDto">VAT nature update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VAT nature DTO or null if not found</returns>
    Task<VatNatureDto?> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateVatNatureDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a VAT nature (soft delete).
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteVatNatureAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}
