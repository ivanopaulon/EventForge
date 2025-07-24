using EventForge.Models.VatRates;
using EventForge.Models.Audit;

namespace EventForge.Services.VatRates;

/// <summary>
/// Service interface for managing VAT rates.
/// </summary>
public interface IVatRateService
{
    /// <summary>
    /// Gets all VAT rates with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of VAT rates</returns>
    Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a VAT rate by ID.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT rate DTO or null if not found</returns>
    Task<VatRateDto?> GetVatRateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new VAT rate.
    /// </summary>
    /// <param name="createVatRateDto">VAT rate creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VAT rate DTO</returns>
    Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createVatRateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing VAT rate.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="updateVatRateDto">VAT rate update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VAT rate DTO or null if not found</returns>
    Task<VatRateDto?> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateVatRateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a VAT rate (soft delete).
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteVatRateAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a VAT rate exists.
    /// </summary>
    /// <param name="vatRateId">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> VatRateExistsAsync(Guid vatRateId, CancellationToken cancellationToken = default);
}