using EventForge.Server.DTOs.Banks;

namespace EventForge.Server.Services.Banks;

/// <summary>
/// Service interface for managing banks.
/// </summary>
public interface IBankService
{
    /// <summary>
    /// Gets all banks with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of banks</returns>
    Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bank by ID.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bank DTO or null if not found</returns>
    Task<BankDto?> GetBankByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new bank.
    /// </summary>
    /// <param name="createBankDto">Bank creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bank DTO</returns>
    Task<BankDto> CreateBankAsync(CreateBankDto createBankDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing bank.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="updateBankDto">Bank update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bank DTO or null if not found</returns>
    Task<BankDto?> UpdateBankAsync(Guid id, UpdateBankDto updateBankDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a bank (soft delete).
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBankAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a bank exists.
    /// </summary>
    /// <param name="bankId">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default);
}