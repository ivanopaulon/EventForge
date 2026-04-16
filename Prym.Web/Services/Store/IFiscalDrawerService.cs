using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service interface for managing fiscal drawers.
/// </summary>
public interface IFiscalDrawerService
{
    // CRUD
    Task<PagedResult<FiscalDrawerDto>?> GetPagedAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default);
    Task<List<FiscalDrawerDto>> GetAllAsync(CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetByPosIdAsync(Guid posId, CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetByOperatorIdAsync(Guid operatorId, CancellationToken ct = default);
    Task<FiscalDrawerDto?> CreateAsync(CreateFiscalDrawerDto dto, CancellationToken ct = default);
    Task<FiscalDrawerDto?> UpdateAsync(Guid id, UpdateFiscalDrawerDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    // Sessions
    Task<FiscalDrawerSessionDto?> GetCurrentSessionAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<PagedResult<FiscalDrawerSessionDto>?> GetSessionsAsync(Guid fiscalDrawerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<FiscalDrawerSessionDto?> OpenSessionAsync(Guid fiscalDrawerId, OpenFiscalDrawerSessionDto dto, CancellationToken ct = default);
    Task<FiscalDrawerSessionDto?> CloseSessionAsync(Guid fiscalDrawerId, CloseFiscalDrawerSessionDto dto, CancellationToken ct = default);

    // Transactions
    Task<PagedResult<FiscalDrawerTransactionDto>?> GetTransactionsAsync(Guid fiscalDrawerId, Guid? sessionId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<FiscalDrawerTransactionDto?> CreateTransactionAsync(Guid fiscalDrawerId, CreateFiscalDrawerTransactionDto dto, CancellationToken ct = default);

    // Cash Denominations
    Task<List<CashDenominationDto>> GetDenominationsAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<List<CashDenominationDto>> InitializeDenominationsAsync(Guid fiscalDrawerId, string currencyCode = "EUR", CancellationToken ct = default);
    Task<CashDenominationDto?> UpdateDenominationAsync(Guid denominationId, UpdateCashDenominationDto dto, CancellationToken ct = default);

    // Change Calculation
    Task<CalculateChangeResponseDto?> CalculateChangeAsync(Guid fiscalDrawerId, CalculateChangeRequestDto request, CancellationToken ct = default);

    // Summary / Dashboard
    Task<FiscalDrawerSummaryDto?> GetSummaryAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<SalesDashboardDto?> GetSalesDashboardAsync(CancellationToken ct = default);
}
